using FluentValidation;
using MediatR;
using TaskManager.Domain.Common;

namespace TaskManager.Application.Common;

public class ValidationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
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
            return await next();

        var context = new ValidationContext<TRequest>(request);

        // ── ASYNC VALIDATION ───────────────────────────
        // ValidateAsync supports async rules (DB checks etc)
        // cancellationToken passed through properly now
        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var errors = validationResults
            .SelectMany(result => result.Errors)
            .Where(error => error != null)
            .ToList();
        // ─────────────────────────────────────────────

        if (errors.Any())
        {
            // ── STRUCTURED ERRORS ──────────────────────
            // List<string> instead of one joined string
            // Easier for frontend to display field-by-field
            var errorMessages = errors
                .Select(e => e.ErrorMessage)
                .ToList();
            // ───────────────────────────────────────────

            // ── NO REFLECTION ──────────────────────────
            // TResponse is constrained to IResult via the
            // 'where TResponse : IResult' below
            // We build the failure response without reflection
            return CreateValidationFailure(errorMessages);
        }

        return await next();
    }

    // ── HELPER ─────────────────────────────────────────
    // Throws a typed exception instead — cleanest, zero reflection
    // Caught by ExceptionHandlingMiddleware automatically
    private static TResponse CreateValidationFailure(List<string> errors)
    {
        throw new FluentValidation.ValidationException(
            errors.Select(e => new FluentValidation.Results.ValidationFailure("", e)));
    }
    // ─────────────────────────────────────────────────
}