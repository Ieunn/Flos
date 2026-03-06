using System.Diagnostics;

namespace Flos.Diagnostics;

/// <summary>
/// Extension methods for <see cref="IProfiler"/> that are compile-stripped
/// when FLOS_PROFILING is not defined, ensuring zero overhead on hot paths.
/// </summary>
/// <remarks>
/// Usage pattern — calls must be matched within try/finally for exception safety:
/// <code>
/// profiler.BeginSampleConditional("MyMethod");
/// try
/// {
///     // ... hot path code ...
/// }
/// finally
/// {
///     profiler.EndSampleConditional();
/// }
/// </code>
/// When FLOS_PROFILING is not defined, both calls are stripped by the compiler.
/// For simple scoping, prefer <c>using (profiler.BeginSample("name")) { ... }</c> directly.
/// </remarks>
public static class ProfilerExtensions
{
    [Conditional("FLOS_PROFILING")]
    public static void BeginSampleConditional(this IProfiler profiler, string name)
    {
        ProfilerSampleStack.Push(profiler.BeginSample(name));
    }

    [Conditional("FLOS_PROFILING")]
    public static void EndSampleConditional(this IProfiler _)
    {
        ProfilerSampleStack.Pop()?.Dispose();
    }
}

/// <summary>
/// Thread-local bounded stack for conditional profiler samples.
/// Capped at <see cref="MaxDepth"/> to prevent unbounded growth from mismatched calls.
/// Only populated when FLOS_PROFILING is defined.
/// </summary>
internal static class ProfilerSampleStack
{
    private const int MaxDepth = 32;

    [ThreadStatic]
    private static IDisposable?[]? _buffer;

    [ThreadStatic]
    private static int _count;

    internal static void Push(IDisposable handle)
    {
        _buffer ??= new IDisposable?[MaxDepth];

        if (_count >= MaxDepth)
            return;

        _buffer[_count++] = handle;
    }

    internal static IDisposable? Pop()
    {
        if (_buffer is null || _count == 0)
            return null;

        var handle = _buffer[--_count];
        _buffer[_count] = null;
        return handle;
    }
}
