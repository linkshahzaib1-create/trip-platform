namespace TripPlatform.Application.Common.Models;

public sealed class Result<T>
{
    public bool IsSuccess { get; init; }
    public T? Value { get; init; }
    public string? Error { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();

    public static Result<T> Success(T value) => new() { IsSuccess = true, Value = value };
    public static Result<T> Failure(string error) => new() { IsSuccess = false, Error = error, Errors = new[] { error } };
    public static Result<T> Failure(IEnumerable<string> errors)
    {
        var list = errors.ToList();
        return new() { IsSuccess = false, Error = list.FirstOrDefault(), Errors = list };
    }
}

public sealed class CostRangeDto
{
    public decimal Min { get; set; }
    public decimal Typical { get; set; }
    public decimal Max { get; set; }
    public string Currency { get; set; } = "USD";
}
