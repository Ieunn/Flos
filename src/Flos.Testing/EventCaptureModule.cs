using Flos.Core.Messaging;
using Flos.Core.Module;
using Flos.Pattern.CQRS;

namespace Flos.Testing;

/// <summary>
/// Internal module that installs a middleware to capture all IEvent
/// instances flowing through the MessageBus.
/// Must be loaded before any module that publishes events.
/// </summary>
internal sealed class EventCaptureModule : ModuleBase
{
    public override string Id => "Testing.EventCapture";

    private readonly EventCaptureMiddleware _middleware = new();

    internal IReadOnlyList<IEvent> CapturedEvents => _middleware.CapturedEvents;

    public override void OnLoad(IServiceScope scope)
    {
        base.OnLoad(scope);
        var bus = scope.Resolve<IMessageBus>();
        bus.Use(_middleware);
    }
}

internal sealed class EventCaptureMiddleware : IMessageMiddleware
{
    private readonly List<IEvent> _capturedEvents = [];

    internal IReadOnlyList<IEvent> CapturedEvents => _capturedEvents;

    public void Handle<T>(T message, Action<T> next) where T : IMessage
    {
        if (message is IEvent evt)
        {
            _capturedEvents.Add(evt);
        }

        next(message);
    }
}
