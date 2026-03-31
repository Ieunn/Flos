namespace Flos.Pattern.CQRS;

/// <summary>Runtime configuration for the CQRS pattern.</summary>
public sealed class CQRSConfig
{
    /// <summary>How applier exceptions are handled. Defaults to <see cref="ApplierFaultMode.Strict"/>.</summary>
    public ApplierFaultMode FaultMode { get; init; } = ApplierFaultMode.Strict;

    /// <summary>Whether events are appended to the <see cref="IEventJournal"/>. Defaults to true.</summary>
    public bool EnableJournal { get; init; } = true;

    /// <summary>
    /// Whether to snapshot world state before applying events for rollback on failure.
    /// Defaults to true (safe). Set to false for high-throughput scenarios where
    /// the per-command snapshot cost is unacceptable and applier correctness is guaranteed.
    /// When false, command handlers receive a zero-copy read-only view of the live world
    /// instead of a deep-cloned snapshot, and applier failures will NOT roll back state.
    /// </summary>
    public bool EnableRollback { get; init; } = true;

    /// <summary>
    /// Maximum number of entries retained in the event journal.
    /// When exceeded, the oldest entries are silently overwritten.
    /// 0 (default) means unbounded — the caller is responsible for manual truncation.
    /// </summary>
    public int MaxJournalEntries { get; init; }

    /// <summary>
    /// Maximum number of deferred commands that may accumulate when <see cref="IPipeline.Send(ICommand)"/>
    /// is called reentrantly (from a handler, applier, or event subscriber).
    /// Deferred commands are executed in FIFO order after the outermost Send completes.
    /// When exceeded, throws <see cref="Flos.Core.Errors.FlosException"/> with <see cref="CQRSErrors.DeferralDepthExceeded"/>.
    /// Default: 16. Set to 0 to disable deferred sends (throws <see cref="CQRSErrors.ReentrantSend"/> immediately).
    /// </summary>
    public int MaxDeferralDepth { get; init; } = 16;
}
