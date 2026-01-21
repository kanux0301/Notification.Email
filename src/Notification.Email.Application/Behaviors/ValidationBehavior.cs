using FluentValidation;
using MediatR;
using Notification.Email.Application.Common;

namespace Notification.Email.Application.Behaviors;

public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : Result
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
            .Where(f => f != null)
            .ToList();

        if (failures.Count != 0)
        {
            var errorMessages = string.Join("; ", failures.Select(f => f.ErrorMessage));
            var error = new Error("Validation.Failed", errorMessages);

            // Create failure result based on TResponse type
            if (typeof(TResponse) == typeof(Result))
            {
                return (TResponse)(object)Result.Failure(error);
            }

            // For Result<T>, we need to use reflection to create the failure
            var resultType = typeof(TResponse);
            if (resultType.IsGenericType && resultType.GetGenericTypeDefinition() == typeof(Result<>))
            {
                var valueType = resultType.GetGenericArguments()[0];
                var failureMethod = typeof(Result)
                    .GetMethod(nameof(Result.Failure), 1, new[] { typeof(Error) })!
                    .MakeGenericMethod(valueType);
                return (TResponse)failureMethod.Invoke(null, new object[] { error })!;
            }

            return (TResponse)(object)Result.Failure(error);
        }

        return await next();
    }
}
