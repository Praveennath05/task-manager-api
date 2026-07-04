using TaskManager.Application.Features.Tasks.Commands;
using TaskManager.Application.Features.Tasks.Validators;
using Xunit;

namespace TaskManager.Application.Tests;

public class CreateTaskValidatorTests
{
    // ── SHARED VALIDATOR INSTANCE ──────────────────────
    // Validators are stateless — safe to reuse across tests
    private readonly CreateTaskValidator _validator = new();

    [Fact]
    public void Validate_EmptyTitle_ShouldFail()
    {
        // ── ARRANGE ────────────────────────────────────
        var command = new CreateTaskCommand(
            Title: "",  // invalid — empty
            Description: "Valid description",
            DueDate: DateTime.UtcNow.AddDays(1));

        // ── ACT ────────────────────────────────────────
        // Validate() runs synchronously, returns a ValidationResult
        var result = _validator.Validate(command);

        // ── ASSERT ─────────────────────────────────────
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Title is required");
    }

    [Fact]
    public void Validate_PastDueDate_ShouldFail()
    {
        var command = new CreateTaskCommand(
            Title: "Valid Title",
            Description: "Valid description",
            DueDate: DateTime.UtcNow.AddDays(-1)); // invalid — in the past

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Due date must be in the future");
    }

    [Fact]
    public void Validate_TitleTooLong_ShouldFail()
    {
        var command = new CreateTaskCommand(
            Title: new string('A', 201), // 201 characters — exceeds 200 max
            Description: "Valid description",
            DueDate: DateTime.UtcNow.AddDays(1));

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Title cannot exceed 200 characters");
    }

    [Fact]
    public void Validate_ValidCommand_ShouldPass()
    {
        // ── IMPORTANT ────────────────────────────────────
        // Always test the "happy path" too — proves the validator
        // doesn't reject perfectly valid data (false positives matter too)
        var command = new CreateTaskCommand(
            Title: "Valid Title",
            Description: "Valid description",
            DueDate: DateTime.UtcNow.AddDays(1));

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }
}