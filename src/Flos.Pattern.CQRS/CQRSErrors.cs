using Flos.Core.Errors;

namespace Flos.Pattern.CQRS;

/// <summary>Error codes for the CQRS pattern (Category 100).</summary>
public static class CQRSErrors
{
    /// <summary>FLOS-100-0001. No handler registered for the command type.</summary>
    public static readonly ErrorCode UnknownCommand = new(100, 1);

    /// <summary>FLOS-100-0002. The command handler returned a failure error code.</summary>
    public static readonly ErrorCode HandlerFailed = new(100, 2);

    /// <summary>FLOS-100-0003. An event applier threw an exception during Apply.</summary>
    public static readonly ErrorCode ApplierFailed = new(100, 3);

    /// <summary>FLOS-100-0004. Reentrant Pipeline.Send detected (e.g., Send called from a handler or applier).</summary>
    public static readonly ErrorCode ReentrantSend = new(100, 4);

    /// <summary>FLOS-100-0005. EnableRollback is true but no IRollbackProvider was registered.</summary>
    public static readonly ErrorCode InvalidConfiguration = new(100, 5);

    /// <summary>A state slice does not implement IDeepCloneable and FaultMode disallows live references.</summary>
    public static readonly ErrorCode SliceNotCloneable = new (100, 6);

    /// <summary>FLOS-100-0007. A requested state slice type was not found in the snapshot.</summary>
    public static readonly ErrorCode SnapshotSliceNotFound = new(100, 7);

    /// <summary>FLOS-100-0008. A state slice does not implement IDeepCloneable for snapshot capture.</summary>
    public static readonly ErrorCode SnapshotNotCloneable = new(100, 8);

    /// <summary>FLOS-100-0009. Deferred command queue depth exceeded MaxDeferralDepth.</summary>
    public static readonly ErrorCode DeferralDepthExceeded = new(100, 9);
}
