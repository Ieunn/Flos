namespace Flos.Pattern.CQRS;

/// <summary>Controls how the pipeline handles exceptions thrown by event appliers.</summary>
public enum ApplierFaultMode : byte
{
    /// <summary>Rolls back state and returns a failure result. Default.</summary>
    Strict,

    /// <summary>Same as Strict but increments a fault counter instead of propagating.</summary>
    Tolerant,

    /// <summary>Throws a <see cref="Flos.Core.Errors.FlosException"/> immediately, crashing the session.</summary>
    Fatal
}
