using Microsoft.EntityFrameworkCore.Update;
using TaskManager .Application.Features.Auth.Commands;
using TaskManager.Application.Features.Auth.Validators;

namespace TaskManager.Application.Tests;

public class LoginValidatorTests
{
    private readonly LoginValidator _validator = new();
    [Fact]
    public void Validate_EmptyEmail_ShouldFail()
    {
        var command = new LoginCommand("","Password123");
        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>e.ErrorMessage == "Email is required"); 
    }
    [Fact]
    public void Validate_InvalidEmailFormat_ShouldFail()
    {
        var command= new LoginCommand("not-an-email","Password123");
        var result = _validator.Validate(command);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Invalid email format");        
    }
    [Fact]
    public void Validate_ValidCommand_ShouldPass()
    {
        var command = new LoginCommand("test@gmail.com", "");
        var result = _validator.Validate(command);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Password is required");
    }
    [Fact]
    public void Validate_validCommand_ShouldPass()
    {
        var command = new LoginCommand("test@gmail.com","anypassword");
        var result = _validator.Validate(command);
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        
    }
}
