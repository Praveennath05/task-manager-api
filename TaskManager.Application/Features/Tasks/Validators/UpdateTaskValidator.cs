using FluentValidation;
using TaskManager.Application.Features.Tasks.Commands;

namespace TaskManager.Application.Features.Tasks.Validators;

public class UpdateTaskValidator : AbstractValidator<UpdateTaskCommand>
{
    public UpdateTaskValidator()
    {
        // ── ID VALIDATION ─────────────────────────────────
        // Id must be greater than 0 — negative or zero Id is invalid
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("Invalid task Id");
        // ─────────────────────────────────────────────────

        // ── TITLE VALIDATION ──────────────────────────────
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(200).WithMessage("Title cannot exceed 200 characters");
        // ─────────────────────────────────────────────────

        // ── DESCRIPTION VALIDATION ────────────────────────
        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters");
        // ─────────────────────────────────────────────────

        // ── DUE DATE VALIDATION ───────────────────────────
       RuleFor(x => x.DueDate)
    .Must(date => date > DateTime.UtcNow)
    .When(x => x.DueDate.HasValue)
    .WithMessage("Due date must be in the future");
        // ─────────────────────────────────────────────────
    }
}