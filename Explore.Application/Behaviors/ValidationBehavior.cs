// ABOUTME: MediatR pipeline behavior that runs FluentValidation validators before command handlers execute.
// ABOUTME: Collects all registered validators for the request type and throws ValidationException on failure.

using FluentValidation;
using MediatR;

namespace Explore.Application.Behaviors;

/// <summary>
/// MediatR pipeline behavior that validates incoming requests using FluentValidation.
/// Collects all registered <see cref="IValidator{TRequest}"/> instances and validates
/// the request before it reaches the handler. Throws <see cref="ValidationException"/>
/// if any validation rules fail.
/// </summary>
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count > 0)
        {
            throw new ValidationException(failures);
        }

        return await next();
    }
}
