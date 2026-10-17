using Krizovka.Users.Domain.Enums;

namespace Krizovka.Users.Domain.Entities;

public class UserVerification
{
    public Guid VerificationId { get; set; }
    public Guid AccountId { get; set; }

    /// <summary>
    /// Name of the attribute being verified (e.g., 'email', 'phone', 'date_of_birth')
    /// </summary>
    public string AttributeName { get; set; } = string.Empty;

    public VerificationStatus Status { get; set; }

    /// <summary>
    /// Method used for verification (e.g., 'EMAIL_LINK', 'SMS_CODE', 'ADMIN_REVIEW')
    /// </summary>
    public string? VerificationMethod { get; set; }

    /// <summary>
    /// Who verified this attribute (e.g., 'SYSTEM', admin email, service name)
    /// </summary>
    public string? VerifiedBy { get; set; }

    public DateTimeOffset? VerifiedAt { get; set; }

    /// <summary>
    /// Optional expiration date for time-limited verifications
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; set; }

    /// <summary>
    /// Additional metadata as JSON (e.g., document ID, rejection reason, notes)
    /// </summary>
    public string? Metadata { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    // Navigation properties
    public UserAccount Account { get; set; } = null!;
}
