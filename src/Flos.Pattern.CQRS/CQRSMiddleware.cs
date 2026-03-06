using Flos.Core.Logging;
using Flos.Core.Messaging;
using Flos.Core.Module;

namespace Flos.Pattern.CQRS;

/// <summary>
/// Middleware that intercepts <see cref="ICommand"/> messages published on the bus
/// and routes them through the CQRS pipeline. Non-command messages pass through.
/// <para>
/// Note: Commands published via <c>bus.Publish(command)</c> go through the non-generic
/// <see cref="IPipeline.Send(ICommand)"/> path, which boxes struct commands. For zero-allocation
/// dispatch, call <see cref="IPipeline.Send{TCommand}"/> directly.
/// </para>
/// </summary>
internal sealed class CQRSMiddleware : IMessageMiddleware
{
    private readonly IPipeline _pipeline;

    internal CQRSMiddleware(IPipeline pipeline)
    {
        _pipeline = pipeline;
    }

    public void Handle<T>(T message, Action<T> next) where T : IMessage
    {
        if (message is ICommand command)
        {
            var result = _pipeline.Send(command);
            if (!result.IsSuccess)
            {
                CoreLog.Warn($"Command '{command.GetType().Name}' rejected via bus.Publish: {result.Error}. " +
                    "Use IPipeline.Send<TCommand>() directly for zero-allocation dispatch and Result access.");
            }
            return;
        }

        next(message);
    }
}

/// <summary>
/// Deferred middleware that resolves the pipeline on first command intercept.
/// Registered during <see cref="CQRSPatternModule.OnLoad"/> (before scope is locked),
/// ensuring middleware is in place before any module can publish messages.
/// </summary>
internal sealed class DeferredCQRSMiddleware : IMessageMiddleware
{
    private readonly IServiceRegistry _scope;
    private IPipeline? _pipeline;

    internal DeferredCQRSMiddleware(IServiceRegistry scope)
    {
        _scope = scope;
    }

    public void Handle<T>(T message, Action<T> next) where T : IMessage
    {
        if (message is ICommand command)
        {
            _pipeline ??= _scope.Resolve<IPipeline>();

            var result = _pipeline.Send(command);
            if (!result.IsSuccess)
            {
                CoreLog.Warn($"Command '{command.GetType().Name}' rejected via bus.Publish: {result.Error}. " +
                    "Use IPipeline.Send<TCommand>() directly for zero-allocation dispatch and Result access.");
            }
            return;
        }

        next(message);
    }
}
