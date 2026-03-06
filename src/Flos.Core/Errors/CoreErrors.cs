namespace Flos.Core.Errors;

/// <summary>
/// Predefined <see cref="ErrorCode"/> constants for the Core microkernel (category 0).
/// </summary>
public static class CoreErrors
{
    public static readonly ErrorCode None = default;
    /// <summary>
    /// FLOS-000-0001. The requested <c>IStateSlice</c> was not found in <c>IWorld</c>.
    /// </summary>
    public static readonly ErrorCode SliceNotFound = new(0, 1);

    /// <summary>
    /// FLOS-000-0010. The service scope is already locked and no further registrations are allowed.
    /// </summary>
    public static readonly ErrorCode ScopeAlreadyLocked = new(0, 10);

    /// <summary>
    /// FLOS-000-0011. The requested service type was not found in the service scope.
    /// </summary>
    public static readonly ErrorCode ServiceNotFound = new(0, 11);

    /// <summary>
    /// FLOS-000-0012. A circular dependency was detected among modules during loading.
    /// </summary>
    public static readonly ErrorCode CircularDependency = new(0, 12);

    /// <summary>
    /// FLOS-000-0020. A required module dependency is missing and could not be resolved.
    /// </summary>
    public static readonly ErrorCode MissingDependency = new(0, 20);

    /// <summary>
    /// FLOS-000-0021. A required pattern dependency is missing and could not be resolved.
    /// </summary>
    public static readonly ErrorCode MissingPattern = new(0, 21);

    /// <summary>
    /// FLOS-000-0022. Session initialization failed (e.g., a module's <c>OnInitialize</c> returned an error).
    /// Session Start failed will also throw this exception.
    /// </summary>
    public static readonly ErrorCode InitializationFailed = new(0, 22);

    /// <summary>
    /// FLOS-000-0030. A tick was dispatched while a previous tick is still executing (reentrant tick).
    /// </summary>
    public static readonly ErrorCode ReentrantTick = new(0, 30);

    /// <summary>
    /// FLOS-000-0031. A message handler threw an exception during <c>Publish</c>.
    /// </summary>
    public static readonly ErrorCode HandlerException = new(0, 31);

    /// <summary>
    /// FLOS-000-0032. A Core subsystem was accessed from a thread other than the one that created it.
    /// </summary>
    public static readonly ErrorCode ThreadViolation = new(0, 32);

    /// <summary>
    /// FLOS-000-0033. A service was registered more than once under the same type key.
    /// </summary>
    public static readonly ErrorCode DuplicateRegistration = new(0, 33);

    /// <summary>
    /// FLOS-000-0040. Accessed <see cref="Result{T}.Value"/> on a failed result, or <see cref="Result{T}.Error"/> on a successful one.
    /// </summary>
    public static readonly ErrorCode InvalidResultAccess = new(0, 40);

    /// <summary>
    /// FLOS-000-0050. A session property was accessed before <see cref="Sessions.Session.Initialize"/> was called.
    /// </summary>
    public static readonly ErrorCode SessionNotInitialized = new(0, 50);

    /// <summary>
    /// FLOS-000-0051. An invalid configuration parameter was provided (e.g., non-positive FixedTimeStep).
    /// </summary>
    public static readonly ErrorCode InvalidConfiguration = new(0, 51);

    /// <summary>
    /// FLOS-000-0052. A session property was accessed after the session was disposed.
    /// </summary>
    public static readonly ErrorCode SessionDisposed = new(0, 52);
}
