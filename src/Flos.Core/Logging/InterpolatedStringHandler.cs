using System.Runtime.CompilerServices;

namespace Flos.Core.Logging;

/// <summary>
/// Interpolated string handler that defers formatting until a log handler is
/// actually attached. When no handler exists, the interpolation never happens
/// — zero allocation, zero formatting.
/// </summary>
[InterpolatedStringHandler]
public ref struct InterpolatedStringHandler
{
    private DefaultInterpolatedStringHandler _inner;

    /// <summary>
    /// Whether any log handler was present at construction time.
    /// When <see langword="false"/>, all Append calls are no-ops.
    /// </summary>
    public bool IsEnabled { get; }

    public InterpolatedStringHandler(
        int literalLength,
        int formattedCount,
        out bool isEnabled)
    {
        // Only allocate the builder when someone is actually listening
        IsEnabled = CoreLog.HasHandler;
        isEnabled = IsEnabled;

        _inner = isEnabled
            ? new DefaultInterpolatedStringHandler(literalLength, formattedCount)
            : default;
    }

    public void AppendLiteral(string s)
    {
        if (IsEnabled) _inner.AppendLiteral(s);
    }

    public void AppendFormatted<T>(T value)
    {
        if (IsEnabled) _inner.AppendFormatted(value);
    }

    public void AppendFormatted<T>(T value, string? format)
    {
        if (IsEnabled) _inner.AppendFormatted(value, format);
    }

    public void AppendFormatted<T>(T value, int alignment)
    {
        if (IsEnabled) _inner.AppendFormatted(value, alignment);
    }

    public void AppendFormatted<T>(T value, int alignment, string? format)
    {
        if (IsEnabled) _inner.AppendFormatted(value, alignment, format);
    }

    public void AppendFormatted(ReadOnlySpan<char> value)
    {
        if (IsEnabled) _inner.AppendFormatted(value);
    }

    public void AppendFormatted(ReadOnlySpan<char> value, int alignment = 0, string? format = null)
    {
        if (IsEnabled) _inner.AppendFormatted(value, alignment, format);
    }

    public void AppendFormatted(string? value)
    {
        if (IsEnabled) _inner.AppendFormatted(value);
    }

    public void AppendFormatted(string? value, int alignment = 0, string? format = null)
    {
        if (IsEnabled) _inner.AppendFormatted(value, alignment, format);
    }

    public void AppendFormatted(object? value, int alignment = 0, string? format = null)
    {
        if (IsEnabled) _inner.AppendFormatted(value, alignment, format);
    }

    internal string ToStringAndClear() => IsEnabled ? _inner.ToStringAndClear() : string.Empty;
}