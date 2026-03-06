namespace Flos.Core.Errors;

/// <summary>
/// Infrastructure-level exception that carries an <see cref="ErrorCode"/>.
/// Thrown for fatal developer bugs and configuration errors that should fail fast on startup.
/// </summary>
public sealed class FlosException : Exception
{
    /// <summary>
    /// Gets the <see cref="ErrorCode"/> associated with this exception.
    /// </summary>
    public ErrorCode Error { get; }

    /// <param name="error">The <see cref="ErrorCode"/> that identifies this error.</param>
    /// <param name="detail">An optional human-readable detail message. When <see langword="null"/>, <see cref="ErrorCode.ToString"/> is used.</param>
    public FlosException(ErrorCode error, string? detail = null)
        : base(detail ?? error.ToString())
    {
        Error = error;
    }

    /// <param name="error">The <see cref="ErrorCode"/> that identifies this error.</param>
    /// <param name="detail">A human-readable detail message.</param>
    /// <param name="innerException">The exception that caused this error.</param>
    public FlosException(ErrorCode error, string detail, Exception innerException)
        : base(detail, innerException)
    {
        Error = error;
    }
}
