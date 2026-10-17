using Krizovka.Users.Domain.Enums;

namespace Krizovka.Users.Infrastructure.Verifications.DTOs;

public record VerificationDto
{
    public string AttributeName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string? VerificationMethod { get; init; }
    public string? VerifiedBy { get; init; }
    public DateTimeOffset? VerifiedAt { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }
    public string? Metadata { get; init; }
}

public record GetVerificationsResponse
{
    public List<VerificationDto> Verifications { get; init; } = new();
}

public record UpdateVerificationRequest
{
    public string Status { get; init; } = string.Empty;
    public string? VerificationMethod { get; init; }
    public string? Metadata { get; init; }
}
