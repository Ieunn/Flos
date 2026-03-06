using System.Diagnostics;
using System.Runtime.CompilerServices;
using Flos.Core.Errors;

namespace Flos.Core.Logging;

/// <summary>
/// Reusable single-thread ownership guard. Captures the creating thread
/// and asserts subsequent calls happen on the same thread.
/// <para>
/// When <see cref="FlosDebug.EnforceThreadSafety"/> is <see langword="true"/>,
/// violations throw <see cref="FlosException"/>. In DEBUG builds with enforcement
/// disabled, a <see cref="Debug.Assert"/> fires instead (non-fatal diagnostic).
/// </para>
/// </summary>
internal readonly struct ThreadGuard
{
    private readonly string _ownerName;
    private readonly int _ownerThreadId;

    internal ThreadGuard(string ownerName)
    {
        _ownerName = ownerName;
        _ownerThreadId = Environment.CurrentManagedThreadId;
    }

    internal void Assert([CallerMemberName] string? caller = null)
    {
        if (Environment.CurrentManagedThreadId == _ownerThreadId)
            return;

        if (FlosDebug.EnforceThreadSafety)
        {
            throw new FlosException(CoreErrors.ThreadViolation,
                $"{_ownerName}.{caller}() called from thread {Environment.CurrentManagedThreadId}, " +
                $"expected main thread {_ownerThreadId}. Use IDispatcher.Enqueue() for cross-thread access.");
        }

        SoftAssert(_ownerName, caller!, _ownerThreadId);
    }

    [Conditional("DEBUG")]
    private static void SoftAssert(string ownerName, string caller, int expectedThread)
    {
        Debug.Assert(false,
            $"{ownerName}.{caller}() called from thread {Environment.CurrentManagedThreadId}, " +
            $"expected main thread {expectedThread}. Use IDispatcher.Enqueue() for cross-thread access.");
    }
}
