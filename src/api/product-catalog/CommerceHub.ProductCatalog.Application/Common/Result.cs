namespace CommerceHub.ProductCatalog.Application.Common;

public class Result
{
    protected Result(Error? error)
    {
        Error = error;
    }

    public Error? Error { get; }

    public bool IsSuccess => Error is null;

    public static Result Success() => new(null);

    public static implicit operator Result(Error error) => new(error);
}

public sealed class Result<T> : Result
{
    private readonly T? _value;

    private Result(T value)
        : base(null)
    {
        _value = value;
    }

    private Result(Error error)
        : base(error)
    {
    }

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access the value of a failed result.");

    public static implicit operator Result<T>(T value) => new(value);

    public static implicit operator Result<T>(Error error) => new(error);
}
