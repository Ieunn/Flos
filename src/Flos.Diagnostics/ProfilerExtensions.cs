using System.Diagnostics;

namespace Flos.Diagnostics;

/// <summary>
/// Extension methods for <see cref="IProfiler"/> that are compile-stripped
/// when FLOS_PROFILING is not defined, ensuring zero overhead on hot paths.
/// </summary>
/// <remarks>
/// Usage pattern:
/// <code>
/// profiler.BeginSampleConditional("MyMethod");
/// // ... hot path code ...
/// profiler.EndSampleConditional();
/// </code>
/// When FLOS_PROFILING is not defined, both calls are stripped by the compiler.
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
/// Thread-local stack for conditional profiler samples.
/// Only populated when FLOS_PROFILING is defined.
/// </summary>
internal static class ProfilerSampleStack
{
    [ThreadStatic]
    private static Stack<IDisposable>? _stack;

    internal static void Push(IDisposable handle)
    {
        _stack ??= new Stack<IDisposable>();
        _stack.Push(handle);
    }

    internal static IDisposable? Pop()
    {
        if (_stack is null || _stack.Count == 0) return null;
        return _stack.Pop();
    }
}
