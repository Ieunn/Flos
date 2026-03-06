namespace Flos.Pattern.CQRS;

/// <summary>Controls how the pipeline handles exceptions thrown by event appliers.</summary>
public enum ApplierFaultMode : byte
{
    /// <summary>Rolls back state and returns a failure result. Default.</summary>
    Strict,

    /// <summary>
    /// Skips the failing applier and continues applying remaining events.
    /// State changes from previously successful appliers are kept.
    /// Increments a fault counter. Does not roll back.
    /// </summary>
    Tolerant,

    /// <summary>Throws a <see cref="Flos.Core.Errors.FlosException"/> immediately, crashing the session.</summary>
    Fatal
}
