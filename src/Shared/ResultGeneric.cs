using System.Diagnostics.CodeAnalysis;

namespace Shared;

public sealed record Result<T>
{
    public T? Value { get; init; }
    public Error? Error { get; init; }

    [MemberNotNullWhen(true, nameof(Error))]
    [MemberNotNullWhen(false, nameof(Value))]
    public bool IsFailure => Error is not null;

    private Result(T value)
    {
        Value = value;
        Error = default;
    }

    private Result(Error error)
    {
        Value = default;
        Error = error;
    }

    public static Result<T> Success(T value) => new(value);
    public static Result<T> Failure(Error error) => new(error);

    public static implicit operator Result<T>(T value) => Success(value);
    public static implicit operator Result<T>(Error error) => Failure(error);

    public static implicit operator T(Result<T> result) 
        => result.Value ?? throw new ArgumentNullException($"{nameof(result.Value)} is null, while {nameof(result.IsFailure)} is '{result.IsFailure}'");
}