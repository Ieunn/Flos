using Flos.Core.Errors;

namespace Flos.Pattern.CQRS;

/// <summary>Error codes for the CQRS pattern (Category 100).</summary>
public static class CQRSErrors
{
    /// <summary>FLOS-100-0001. No handler registered for the command type.</summary>
    public static readonly ErrorCode UnknownCommand = new(100, 1);

    /// <summary>FLOS-100-0002. The command handler returned a failure result.</summary>
    public static readonly ErrorCode HandlerFailed = new(100, 2);

    /// <summary>FLOS-100-0003. An event applier threw an exception during Apply.</summary>
    public static readonly ErrorCode ApplierFailed = new(100, 3);

    /// <summary>FLOS-100-0004. A handler for this command type is already registered.</summary>
    public static readonly ErrorCode DuplicateHandler = new(100, 4);
}
