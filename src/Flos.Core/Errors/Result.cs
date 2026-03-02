namespace Flos.Core.Errors;

/// <summary>
/// A discriminated-union result type that holds either a success value of type <typeparamref name="T"/>
/// or a failure <see cref="ErrorCode"/>. Used for expected domain failures instead of exceptions.
/// </summary>
/// <typeparam name="T">The type of the success value.</typeparam>
public readonly record struct Result<T>
{
    private readonly T? _value;
    private readonly ErrorCode _error;

    /// <summary>
    /// Gets a value indicating whether this result represents a success.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets the success value.
    /// </summary>
    /// <exception cref="FlosException">Thrown when <see cref="IsSuccess"/> is <see langword="false"/>.</exception>
    public T Value => IsSuccess
        ? _value! : throw new FlosException(_error, "Accessed Value on failed Result.");

    /// <summary>
    /// Gets the <see cref="ErrorCode"/> describing the failure.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when <see cref="IsSuccess"/> is <see langword="true"/>.</exception>
    public ErrorCode Error => !IsSuccess
        ? _error : throw new InvalidOperationException("Accessed Error on successful Result.");

    private Result(T value) { IsSuccess = true; _value = value; _error = default; }
    private Result(ErrorCode error) { IsSuccess = false; _value = default; _error = error; }

    /// <summary>
    /// Creates a successful result containing the specified value.
    /// </summary>
    /// <param name="value">The success value.</param>
    /// <returns>A <see cref="Result{T}"/> with <see cref="IsSuccess"/> set to <see langword="true"/>.</returns>
    public static Result<T> Ok(T value) => new(value);

    /// <summary>
    /// Creates a failed result containing the specified error code.
    /// </summary>
    /// <param name="error">The <see cref="ErrorCode"/> describing the failure.</param>
    /// <returns>A <see cref="Result{T}"/> with <see cref="IsSuccess"/> set to <see langword="false"/>.</returns>
    public static Result<T> Fail(ErrorCode error) => new(error);

    /// <summary>
    /// Transforms the success value using the given mapping function.
    /// If this result is a failure, the error is propagated unchanged.
    /// </summary>
    /// <typeparam name="U">The type of the mapped value.</typeparam>
    /// <param name="map">The function to apply to the success value.</param>
    /// <returns>A new result containing the mapped value, or the original error.</returns>
    public Result<U> Map<U>(Func<T, U> map) =>
        IsSuccess ? Result<U>.Ok(map(_value!)) : Result<U>.Fail(_error);

    /// <summary>
    /// Chains a result-producing function onto a success value (monadic bind / flatMap).
    /// If this result is a failure, the error is propagated unchanged.
    /// </summary>
    /// <typeparam name="U">The type of the bound value.</typeparam>
    /// <param name="bind">The function that returns a new <see cref="Result{U}"/>.</param>
    /// <returns>The result of the bind function, or the original error.</returns>
    public Result<U> Bind<U>(Func<T, Result<U>> bind) =>
        IsSuccess ? bind(_value!) : Result<U>.Fail(_error);

    /// <summary>
    /// Pattern-matches on this result, invoking <paramref name="onOk"/> for success
    /// or <paramref name="onFail"/> for failure.
    /// </summary>
    /// <typeparam name="U">The return type of both branches.</typeparam>
    /// <param name="onOk">Function invoked with the success value.</param>
    /// <param name="onFail">Function invoked with the error code.</param>
    /// <returns>The result of the matched branch.</returns>
    public U Match<U>(Func<T, U> onOk, Func<ErrorCode, U> onFail) =>
        IsSuccess ? onOk(_value!) : onFail(_error);

    /// <summary>
    /// Implicitly converts an <see cref="ErrorCode"/> to a failed <see cref="Result{T}"/>.
    /// Enables ergonomic failure returns: <c>return CQRSErrors.HandlerFailed;</c>
    /// </summary>
    /// <param name="error">The error code.</param>
    public static implicit operator Result<T>(ErrorCode error) => Fail(error);
}
