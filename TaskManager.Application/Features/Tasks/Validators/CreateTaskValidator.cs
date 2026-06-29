using FluentValidation;
using TaskManager.Application.Features.Tasks.Commands;

namespace TaskManager.Application.Features.Tasks.Validators;

public class CreateTaskValidator : AbstractValidator<CreateTaskCommand>
{
    public CreateTaskValidator()
    {
        // ── FLUENT VALIDATION RULES ───────────────────────
        // Instead of if/else checks in the handler,
        // we define rules here — clean and readable
        // These run automatically before the handler executes

        // Title must not be empty and max 200 characters
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(200).WithMessage("Title cannot exceed 200 characters");

        // Description max 1000 characters — optional field
        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters");

        // DueDate must be in the future if provided
        RuleFor(x => x.DueDate)
    .Must(date => date > DateTime.UtcNow)
    .When(x => x.DueDate.HasValue)
    .WithMessage("Due date must be in the future");
        // ─────────────────────────────────────────────────
    }
}