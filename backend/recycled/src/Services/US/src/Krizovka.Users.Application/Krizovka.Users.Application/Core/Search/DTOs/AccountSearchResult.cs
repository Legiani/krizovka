using Krizovka.Users.Domain.Enums;

namespace Krizovka.Users.Application.Core.Search.DTOs;

public record SearchPagedResponse(List<AccountSearchResult> Accounts, int Total);

public class AccountSearchResult
{
    public Guid AccountId { get; set; }
    public AccountType AccountType { get; set; }
    public AccountStatus Status { get; set; }
    public Guid? IdentityId { get; set; }
    public string? Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string MatchedBy { get; set; } = string.Empty;
}
