using Krizovka.Users.Domain.Enums;

namespace Krizovka.Users.Domain.Entities;

public class AccountClaim
{
    public Guid ClaimId { get; set; }
    public Guid AccountId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string ClaimToken { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public ClaimStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }

    // Navigation properties
    public UserAccount Account { get; set; } = null!;
}
