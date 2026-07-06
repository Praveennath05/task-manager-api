using TaskManager.Application.Features.Tasks.Commands;
using TaskManager.Application.Features.Tasks.Validators;
using Xunit;

namespace TaskManager.Application.Tests;

public class UpdateTaskValidatorTests
{
    private readonly UpdateTaskValidator _validator = new();

    [Fact]
    public void Validate_ZeroOrNegativeId_ShouldFail()
    {
        var command = new UpdateTaskCommand(
            Id: 0, // invalid — must be greater than 0
            Title: "Valid Title",
            Description: "Valid description",
            IsCompleted: false,
            DueDate: DateTime.UtcNow.AddDays(1));

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Invalid task Id");
    }

    [Fact]
    public void Validate_EmptyTitle_ShouldFail()
    {
        var command = new UpdateTaskCommand(
            Id: 1,
            Title: "",
            Description: "Valid description",
            IsCompleted: false,
            DueDate: DateTime.UtcNow.AddDays(1));

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Title is required");
    }

    [Fact]
    public void Validate_PastDueDate_ShouldFail()
    {
        var command = new UpdateTaskCommand(
            Id: 1,
            Title: "Valid Title",
            Description: "Valid description",
            IsCompleted: false,
            DueDate: DateTime.UtcNow.AddDays(-1));

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Due date must be in the future");
    }

    [Fact]
    public void Validate_ValidCommand_ShouldPass()
    {
        var command = new UpdateTaskCommand(
            Id: 1,
            Title: "Valid Title",
            Description: "Valid description",
            IsCompleted: true,
            DueDate: DateTime.UtcNow.AddDays(1));

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }
}