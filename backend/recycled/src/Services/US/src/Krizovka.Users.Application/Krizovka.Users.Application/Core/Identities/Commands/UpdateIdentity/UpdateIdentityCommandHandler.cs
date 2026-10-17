using System.Net.Http.Json;
using Krizovka.Shared.ErrorHandling.Exceptions;
using System.Text.Json.Serialization;
using Krizovka.Users.Application.Core.Interfaces.Persistence;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Krizovka.Users.Application.Core.Identities.Commands.UpdateIdentity;

public class UpdateIdentityCommandHandler : IRequestHandler<UpdateIdentityCommand>
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<UpdateIdentityCommandHandler> _logger;
    private readonly IUserAccountRepository _accountRepository;

    public UpdateIdentityCommandHandler(IHttpClientFactory httpClientFactory, ILogger<UpdateIdentityCommandHandler> logger, IUserAccountRepository accountRepository)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _accountRepository = accountRepository;
    }

    public async Task Handle(UpdateIdentityCommand request, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient("KratosAdmin");

        // 1. Fetch current identity to preserve other traits if needed (like dob)
        // Or we can just patch/put what we have. Kratos usually requires full traits object for PUT.
        // Let's fetch first.
        var getResponse = await client.GetAsync($"/admin/identities/{request.IdentityId}", cancellationToken);
        if (!getResponse.IsSuccessStatusCode)
        {
            throw new NotFoundException($"Identity {request.IdentityId} not found in Kratos.");
        }

        var identity = await getResponse.Content.ReadFromJsonAsync<KratosIdentityDto>(cancellationToken: cancellationToken);
        if (identity == null) throw new DomainException("Failed to deserialize identity.");

        // 2. Update Traits
        // We assume the schema structure is standard (email, name.first, name.last)
        if (identity.Traits == null) identity.Traits = new KratosTraits();
        if (identity.Traits.Name == null) identity.Traits.Name = new KratosName();

        identity.Traits.Email = request.Email;
        identity.Traits.Name.First = request.FirstName;
        identity.Traits.Name.Last = request.LastName;
        if (request.Phone != null) identity.Traits.Phone = request.Phone;
        if (request.DateOfBirth != null) identity.Traits.DateOfBirth = request.DateOfBirth;

        // 3. Send Update (PUT)
        // Kratos API: PUT /admin/identities/{id}
        // Body: { traits: {...}, ... }
        // We can send the modified schema.

        var updatePayload = new
        {
            schema_id = "default", // or identity.SchemaId
            traits = identity.Traits,
            state = identity.State // Preserve state
        };

        var response = await client.PutAsJsonAsync($"/admin/identities/{request.IdentityId}", updatePayload, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Failed to update identity in Kratos: {Error}", error);
            throw new DomainException($"Failed to update identity: {response.StatusCode}");
        }

        // Sync back to local UserAccount DB cache
        var account = await _accountRepository.GetByIdentityAsync(request.IdentityId, cancellationToken);
        if (account != null)
        {
            account.Email = request.Email;
            account.FirstName = request.FirstName;
            account.LastName = request.LastName;
            account.UpdatedAt = DateTimeOffset.UtcNow;
            await _accountRepository.UpdateAsync(account, cancellationToken);
            _logger.LogInformation("Synced identity {IdentityId} profile to local UserAccount DB.", request.IdentityId);
        }
    }

    private class KratosIdentityDto
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }
        [JsonPropertyName("traits")]
        public KratosTraits? Traits { get; set; }
        [JsonPropertyName("state")]
        public string? State { get; set; }
        [JsonPropertyName("schema_id")]
        public string? SchemaId { get; set; }
    }

    private class KratosTraits
    {
        [JsonPropertyName("email")]
        public string? Email { get; set; }
        [JsonPropertyName("name")]
        public KratosName? Name { get; set; }

        [JsonPropertyName("phone")]
        public string? Phone { get; set; }

        [JsonPropertyName("date_of_birth")]
        public string? DateOfBirth { get; set; }

        // Preserve other fields loosely if we used JsonElement or object,
        // but here rigid mapping might lose data if schema varies?
        // Ideally we should use dynamic or JsonNode to patch only fields we know
        // without losing others.
        // But for this task, let's assume standard schema.
        // To be safer, we can try to use JsonNode if available in .NET 6+
        [JsonExtensionData]
        public Dictionary<string, object>? ValidFrom { get; set; } // Catch-all?
    }

    private class KratosName
    {
        [JsonPropertyName("first")]
        public string? First { get; set; }
        [JsonPropertyName("last")]
        public string? Last { get; set; }
    }
}
