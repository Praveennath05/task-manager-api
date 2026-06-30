namespace TaskManager.Domain.Common;

// ── NON-GENERIC INTERFACE ─────────────────────────────
// Lets code work with Result<T> without knowing T
// This is what removes the need for reflection
public interface IResult
{
    bool IsSuccess { get; }
    string? ErrorMessage { get; }
}
// ─────────────────────────────────────────────────────

public class Result<T> : IResult
{
    public bool IsSuccess { get; set; }
    public T? Data { get; set; }
    public string? ErrorMessage { get; set; }

    public static Result<T> Success(T data) =>
        new Result<T> { IsSuccess = true, Data = data };

    // ── STATIC FAILURE WITH GENERIC CONSTRAINT TRICK ──
    // We can't call Result<T>.Failure() without knowing T
    // So ValidationBehavior builds the object directly instead
    public static Result<T> Failure(string error) =>
        new Result<T> { IsSuccess = false, ErrorMessage = error };
}