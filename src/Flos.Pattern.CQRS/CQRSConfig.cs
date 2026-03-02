namespace Flos.Pattern.CQRS;

/// <summary>Runtime configuration for the CQRS pattern.</summary>
public sealed class CQRSConfig
{
    /// <summary>How applier exceptions are handled. Defaults to <see cref="ApplierFaultMode.Strict"/>.</summary>
    public ApplierFaultMode FaultMode { get; set; } = ApplierFaultMode.Strict;

    /// <summary>Whether events are appended to the <see cref="IEventJournal"/>. Defaults to true.</summary>
    public bool EnableJournal { get; set; } = true;
}
