using Flos.Core.Logging;
using Flos.Core.Messaging;

namespace Flos.Pattern.CQRS;

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
            if (!result.IsSuccess && CoreLog.Handler is not null)
            {
                CoreLog.Warn($"Command '{command.GetType().Name}' rejected via bus.Publish: {result.Error}. " +
                    "Use IPipeline.Send() directly to get the Result.");
            }
            return;
        }

        next(message);
    }
}
