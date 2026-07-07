using TaskManager.Application.Features.Auth.Commands;
using TaskManager.Application.Features.Auth.Validators;
using Xunit;

namespace TaskManager.Application.Tests;

public class RegisterValidatorTests
{
    private readonly RegisterValidator _validator = new();

    [Fact]
    public void Validate_InvalidEmailFormat_ShouldFail()
    {
        var command = new RegisterCommand("not-an-email", "Password123");

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Invalid email format");
    }

    [Fact]
    public void Validate_PasswordTooShort_ShouldFail()
    {
        var command = new RegisterCommand("test@gmail.com", "Ab1");

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Password must be at least 8 characters");
    }

    [Fact]
    public void Validate_PasswordMissingUppercase_ShouldFail()
    {
        var command = new RegisterCommand("test@gmail.com", "password123");

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Password must contain an uppercase letter");
    }

    [Fact]
    public void Validate_PasswordMissingDigit_ShouldFail()
    {
        var command = new RegisterCommand("test@gmail.com", "Password");

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Password must contain a digit");
    }

    [Fact]
    public void Validate_ValidCommand_ShouldPass()
    {
        var command = new RegisterCommand("test@gmail.com", "Password123");

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }
}