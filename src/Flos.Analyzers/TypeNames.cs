namespace Flos.Analyzers;

/// <summary>
/// Fully qualified type names used by analyzers for symbol matching.
/// </summary>
internal static class TypeNames
{
    public const string SystemRandom = "System.Random";
    public const string DateTime = "System.DateTime";
    public const string DateTimeOffset = "System.DateTimeOffset";
    public const string Environment = "System.Environment";
    public const string Stopwatch = "System.Diagnostics.Stopwatch";
    public const string Guid = "System.Guid";

    public const string Dictionary = "System.Collections.Generic.Dictionary";
    public const string HashSet = "System.Collections.Generic.HashSet";

    public const string File = "System.IO.File";
    public const string Directory = "System.IO.Directory";
    public const string Socket = "System.Net.Sockets.Socket";
    public const string HttpClient = "System.Net.Http.HttpClient";
    public const string WebRequest = "System.Net.WebRequest";

    public const string Parallel = "System.Threading.Tasks.Parallel";
    public const string Task = "System.Threading.Tasks.Task";

    public const string ICommandHandlerName = "ICommandHandler";
    public const string ICommandHandlerNamespace = "Flos.Pattern.CQRS";
    public const int ICommandHandlerArity = 1;

    public const string IEventApplierName = "IEventApplier";
    public const string IEventApplierNamespace = "Flos.Pattern.CQRS";
    public const int IEventApplierArity = 2;

    public const string HotPathAttributeFullName = "Flos.Core.Annotations.HotPathAttribute";

    public const string IMessageBus = "Flos.Core.Messaging.IMessageBus";
    public const string IWorld = "Flos.Core.State.IWorld";
    public const string IStateSlice = "Flos.Core.State.IStateSlice";
    public const string IServiceScope = "Flos.Core.Module.IServiceScope";

    public const string IRandom = "Flos.Random.IRandom";

    public const string ICommand = "Flos.Pattern.CQRS.ICommand";
    public const string IEvent = "Flos.Pattern.CQRS.IEvent";
}
