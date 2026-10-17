using FluentValidation;
using MediatR;
using ApiException = Krizovka.Shared.ErrorHandling.Exceptions.ValidationException;

namespace Krizovka.Shared.ErrorHandling.Behaviors;

public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (_validators.Any())
        {
            var context = new ValidationContext<TRequest>(request);

            var validationResults = await Task.WhenAll(
                _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

            var failures = validationResults
                .Where(r => r.Errors.Any())
                .SelectMany(r => r.Errors)
                .ToList();

            if (failures.Any())
            {
                var errors = failures
                    .Where(x => x != null)
                    .GroupBy(x => x.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(x => x.ErrorMessage).ToArray()
                    );

                throw new ApiException(errors);
            }
        }

        return await next();
    }
}
