using Krizovka.Users.Application.Core.Context.DTOs;
using MediatR;

namespace Krizovka.Users.Application.Core.Context.Queries.ValidateContext;

public record ValidateTokenQuery(string Token) : IRequest<ContextValidationResult>;
