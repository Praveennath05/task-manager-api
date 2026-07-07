using System.Data;
using FluentValidation;
using TaskManager.Application.Features.Auth.Commands;

namespace TaskManager.Application.Features.Auth.Validators;

public class LoginValidator : AbstractValidator<LoginCommand>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email)
        .NotEmpty().WithMessage("Email is required")
        .EmailAddress().WithMessage("Invalid email format");

        RuleFor(x => x.Password)
        .NotEmpty().WithMessage("Password is required");  
    }
}
