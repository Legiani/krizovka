using Krizovka.Users.Domain.Entities;
using MediatR;

namespace Krizovka.Users.Application.Core.Claims.Queries.GetClaim;

public record GetAccountClaimQuery(Guid ClaimId) : IRequest<AccountClaim?>;
