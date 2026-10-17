using System.Net.Http.Json;
using Krizovka.Shared.ErrorHandling.Exceptions;
using Krizovka.Users.Application.Core.Interfaces.Persistence;
using Krizovka.Users.Domain.Entities;
using MediatR;

namespace Krizovka.Users.Application.Core.Backoffice.Commands.CreateBackofficeUser;

public class CreateBackofficeUserHandler : IRequestHandler<CreateBackofficeUserCommand, Guid>
{
    private readonly IBackofficeUserRepository _userRepository;
    private readonly IBackofficeRoleRepository _roleRepository;
    private readonly IUserAccountRepository _accountRepository;
    private readonly IHttpClientFactory _httpClientFactory;

    public CreateBackofficeUserHandler(
        IBackofficeUserRepository userRepository,
        IBackofficeRoleRepository roleRepository,
        IUserAccountRepository accountRepository,
        IHttpClientFactory httpClientFactory)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _accountRepository = accountRepository;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<Guid> Handle(CreateBackofficeUserCommand request, CancellationToken cancellationToken)
    {
        // 1. Duplicity Checks
        if (await _userRepository.ExistsByEmailAsync(request.WorkingEmail, cancellationToken))
        {
            // Using Exception for 409, assuming Global Exception Handler maps this or we should use a specific exception
            throw new ConflictException($"Backoffice user with email {request.WorkingEmail} already exists.");
        }

        // 2. Start Transaction
        // We need IoC for DbContext or IUnitOfWork to manage transaction explicitly.
        // Assuming _userRepository might not expose Transaction.
        // But we can rely on implicit flow if we simply "Don't Save" until valid?
        // No, user explicitly asked for "rollback DB transaction".
        // Let's assume we can't easily access DbContext transaction here via Repository pattern without breaking abstraction
        // UNLESS we inject DbContext or IUnitOfWork.
        // The handler constructor doesn't have it.
        // For this task, I will instantiate a transaction scope or assume _userRepository.AddAsync saves immediately (it does based on previous view_file).
        // Since AddAsync calls SaveChangesAsync, we MUST use a transaction to be able to rollback.
        // Accessing DbContext from Repository is internal.
        // I will injection UsDbContext if possible or use TransactionScope.
        // Let's use TransactionScope (System.Transactions) which EF Core supports.

        // Wait, I cannot inject DbContext easily if it's not registered or if I want to keep architecture clean.
        // But wait, DependencyInjection.cs registered UsDbContext. So I can inject it.
        // I'll add UsDbContext to constructor.

        // However, I can't change constructor easily without updating the file fully?
        // I will try to use TransactionScope.

        // RE-READING: "BackofficeUser již s WorkingEmail? -> 409".
        // "BackofficeUser s IdentityId? -> 409".
        // "Po PATCH identity kontroluj IsSuccessStatusCode; při chybě rollbackuj DB transakci."

        using var transactionScope = new System.Transactions.TransactionScope(System.Transactions.TransactionScopeAsyncFlowOption.Enabled);

        try
        {
            var client = _httpClientFactory.CreateClient("KratosAdmin");
            Guid identityId = Guid.Empty;

            // Search by email first to see if Identity exists
            var searchResponse = await client.GetAsync($"/admin/identities?credentials_identifier={request.WorkingEmail}", cancellationToken);
            if (searchResponse.IsSuccessStatusCode)
            {
                var identities = await searchResponse.Content.ReadFromJsonAsync<List<KratosIdentityResponse>>(cancellationToken: cancellationToken);
                if (identities != null && identities.Count > 0)
                {
                    identityId = identities[0].Id;

                    // Check if this Identity is already used by another BackofficeUser
                    var existingUser = await _userRepository.GetByIdentityIdAsync(identityId, cancellationToken);
                    if (existingUser != null)
                    {
                         throw new ConflictException($"Backoffice user with IdentityId {identityId} already exists.");
                    }
                }
            }

            // Create Kratos Identity if not exists
            if (identityId == Guid.Empty)
            {
                var identityBody = new
                {
                    schema_id = "default",
                    traits = new
                    {
                        email = request.WorkingEmail,
                        name = new { first = request.FirstName, last = request.LastName }
                    }
                };

                var kratosResponse = await client.PostAsJsonAsync("/admin/identities", identityBody, cancellationToken);
                if (!kratosResponse.IsSuccessStatusCode)
                {
                    var error = await kratosResponse.Content.ReadAsStringAsync(cancellationToken);
                    throw new DomainException($"Failed to create Kratos identity: {kratosResponse.StatusCode} - {error}");
                }

                var kratosIdentity = await kratosResponse.Content.ReadFromJsonAsync<KratosIdentityResponse>(cancellationToken: cancellationToken);
                identityId = kratosIdentity?.Id ?? Guid.Empty;
            }

            // Create DB User
            var user = new BackofficeUser
            {
                Id = Guid.NewGuid(),
                IdentityId = identityId,
                AccountId = request.AccountId, // Is this correct? Requirement said "AccountId = null nebo AccountId = boUser.AccountId pokud existuje" for VALIDATION. For CREATION, we take from request.
                Email = request.WorkingEmail,
                FirstName = request.FirstName,
                LastName = request.LastName,
                IsSuperAdmin = request.IsSuperAdmin,
                IsActive = true, // Default to true on create?
                RoleId = request.RoleId
            };

            await _userRepository.AddAsync(user, cancellationToken);

            // Sync/Patch Kratos Identity (ensure traits are up to date)
            var updateTraitsBody = new
            {
                traits = new
                {
                    email = request.WorkingEmail, // Ensure email is kept
                    name = new { first = request.FirstName, last = request.LastName }
                }
            };

            var patchResponse = await client.PatchAsJsonAsync($"/admin/identities/{identityId}", updateTraitsBody, cancellationToken);

            if (!patchResponse.IsSuccessStatusCode)
            {
                // Kratos Failed -> Rollback DB
                // Since we are in TransactionScope and will NOT call Complete(), it rolls back.
                var error = await patchResponse.Content.ReadAsStringAsync(cancellationToken);
                throw new DomainException($"Failed to patch Kratos identity: {patchResponse.StatusCode} - {error}");
            }

            // Success
            transactionScope.Complete();
            return user.Id;
        }
        catch (Exception)
        {
            // TransactionScope disposes and rolls back automatically
            throw;
        }
    }

    private class KratosIdentityResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("id")]
        public Guid Id { get; set; }
    }
}
