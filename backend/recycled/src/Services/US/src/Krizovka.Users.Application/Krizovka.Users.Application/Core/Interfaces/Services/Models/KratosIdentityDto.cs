using System.Text.Json.Serialization;

namespace Krizovka.Users.Application.Core.Interfaces.Services.Models;

public class KratosIdentityDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("traits")]
    public KratosTraits Traits { get; set; } = new();
}

public class KratosTraits
{
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("name")]
    public KratosName? Name { get; set; }

    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    [JsonPropertyName("date_of_birth")]
    public string? DateOfBirth { get; set; }

    [JsonPropertyName("registration_system_id")]
    public string? RegistrationSystemId { get; set; }
}

public class KratosName
{
    [JsonPropertyName("first")]
    public string? First { get; set; }

    [JsonPropertyName("last")]
    public string? Last { get; set; }
}
