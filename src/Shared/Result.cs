using System.Diagnostics.CodeAnalysis;

namespace Shared;

public sealed record Result
{
    public Error? Error { get; init; }

    [MemberNotNullWhen(true, nameof(Error))]
    public bool IsFailure => Error is not null;

    private Result()
    {
        Error = null;
    }

    private Result(Error error)
    {
        Error = error;
    }

    public static Result Success() => new();
    public static Result Failure(Error err) => new(err);

    public static implicit operator Result(Error err) => new(err);
}