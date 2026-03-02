namespace Flos.Core.Errors;

/// <summary>
/// Predefined <see cref="ErrorCode"/> constants for the Core microkernel (category 0).
/// </summary>
public static class CoreErrors
{
    /// <summary>
    /// FLOS-000-0001. The requested <c>IStateSlice</c> was not found in <c>IWorld</c>.
    /// </summary>
    public static readonly ErrorCode SliceNotFound = new(0, 1);

    /// <summary>
    /// FLOS-000-0002. An <c>IStateSlice</c> of the same type has already been registered in <c>IWorld</c>.
    /// </summary>
    public static readonly ErrorCode DuplicateSlice = new(0, 2);

    /// <summary>
    /// FLOS-000-0010. The service scope is already locked and no further registrations are allowed.
    /// </summary>
    public static readonly ErrorCode ScopeAlreadyLocked = new(0, 10);

    /// <summary>
    /// FLOS-000-0011. The requested service type was not found in the service scope.
    /// </summary>
    public static readonly ErrorCode ServiceNotFound = new(0, 11);

    /// <summary>
    /// FLOS-000-0012. The service type cannot be instantiated (e.g., it is abstract or has no suitable constructor).
    /// </summary>
    public static readonly ErrorCode CannotInstantiate = new(0, 12);

    /// <summary>
    /// FLOS-000-0020. A circular dependency was detected among modules during loading.
    /// </summary>
    public static readonly ErrorCode CircularDependency = new(0, 20);

    /// <summary>
    /// FLOS-000-0021. A required module dependency is missing and could not be resolved.
    /// </summary>
    public static readonly ErrorCode MissingDependency = new(0, 21);

    /// <summary>
    /// FLOS-000-0022. A required pattern dependency is missing and could not be resolved.
    /// </summary>
    public static readonly ErrorCode MissingPattern = new(0, 22);

    /// <summary>
    /// FLOS-000-0015. Middleware was added to <c>IMessageBus</c> after the first message was published.
    /// </summary>
    public static readonly ErrorCode MiddlewareAfterPublish = new(0, 15);

    /// <summary>
    /// FLOS-000-0030. A tick was dispatched while a previous tick is still executing (reentrant tick).
    /// </summary>
    public static readonly ErrorCode ReentrantTick = new(0, 30);

    /// <summary>
    /// FLOS-000-0040. The requested session state transition is not valid from the current state.
    /// </summary>
    public static readonly ErrorCode InvalidStateTransition = new(0, 40);

    /// <summary>
    /// FLOS-000-0041. Session initialization failed (e.g., a module's <c>OnInitialize</c> returned an error).
    /// </summary>
    public static readonly ErrorCode InitializationFailed = new(0, 41);
}
