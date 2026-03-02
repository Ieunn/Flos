namespace Flos.Core.Errors;

/// <summary>
/// Infrastructure-level exception that carries an <see cref="ErrorCode"/>.
/// Thrown for fatal developer bugs and configuration errors that should fail fast on startup.
/// </summary>
/// <param name="error">The <see cref="ErrorCode"/> that identifies this error.</param>
/// <param name="detail">An optional human-readable detail message. When <see langword="null"/>, <see cref="ErrorCode.ToString"/> is used.</param>
public sealed class FlosException(ErrorCode error, string? detail = null)
    : Exception(detail ?? error.ToString())
{
    /// <summary>
    /// Gets the <see cref="ErrorCode"/> associated with this exception.
    /// </summary>
    public ErrorCode Error { get; } = error;
}
