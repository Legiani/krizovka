using Krizovka.Users.Application.Core.Context.Queries.ValidateContext;
using MediatR;
using Krizovka.Users.Application.Core.Context.DTOs;

namespace Krizovka.Users.Application.Core.Context.Queries.BatchValidateContext;

public record BatchValidateContextQuery(List<ValidationCheck> Checks) : IRequest<List<ValidationResult>>;

public record ValidationCheck(Guid Aid, string Operation);
public record ValidationResult(Guid Aid, string Operation, bool Allowed, ScopeInfo? Scope);

public class BatchValidateContextQueryHandler : IRequestHandler<BatchValidateContextQuery, List<ValidationResult>>
{
    private readonly IMediator _mediator;

    public BatchValidateContextQueryHandler(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<List<ValidationResult>> Handle(BatchValidateContextQuery request, CancellationToken cancellationToken)
    {
        var results = new List<ValidationResult>();
        foreach (var check in request.Checks)
        {
            // Re-use single validation logic
            var result = await _mediator.Send(new ValidateContextQuery(check.Aid, check.Operation), cancellationToken);
            results.Add(new ValidationResult(check.Aid, check.Operation, result.Allowed, result.Scope));
        }
        return results;
    }
}
