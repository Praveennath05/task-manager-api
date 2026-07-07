using FluentValidation;
using TaskManager.Application.Features.Auth.Commands;

namespace TaskManager.Application.Features.Auth.Validators;

public class RegisterValidator : AbstractValidator<RegisterCommand>
{
    public RegisterValidator()
    {
        RuleFor(x => x.Email)
        .NotEmpty().WithMessage("Email is required")
        .EmailAddress().WithMessage("Invalid email format");

        RuleFor(x => x.Password)
        .NotEmpty().WithMessage("Password is required")
        .MinimumLength(8).WithMessage("Password must be at least 8 characters")
        .Matches("[A-Z]").WithMessage("Password must contain an uppercase letter")
        .Matches("[0-9]").WithMessage("Password must contain a digit");


    }
}
