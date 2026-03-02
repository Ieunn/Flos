using Flos.Core.Logging;
using Flos.Core.Messaging;
using Flos.Core.Module;
using Flos.Core.Scheduling;
using Flos.Core.Sessions;

namespace Flos.Adapter.Console;

/// <summary>
/// Message published when a line is read from stdin.
/// </summary>
public readonly record struct ConsoleInputMessage(string Line) : IMessage;

/// <summary>
/// Publish this message to write a line to stdout.
/// </summary>
public readonly record struct ConsoleOutputMessage(string Line) : IMessage;

/// <summary>
/// Console adapter module.
/// Bridges CoreLog→stderr, stdin→ConsoleInputMessage (via IDispatcher), ConsoleOutputMessage→stdout.
/// </summary>
public sealed class ConsoleAdapterModule : ModuleBase
{
    public override string Id => "Adapter.Console";

    private IDispatcher? _dispatcher;
    private IMessageBus? _bus;
    private IDisposable? _outputSubscription;
    private Thread? _inputThread;
    private volatile bool _running;
    private TextWriter _stdout = System.Console.Out;
    private TextWriter _stderr = System.Console.Error;
    private TextReader _stdin = System.Console.In;

    /// <summary>
    /// Allow injection of custom I/O streams for testing.
    /// </summary>
    public ConsoleAdapterModule() { }

    public ConsoleAdapterModule(TextReader stdin, TextWriter stdout, TextWriter stderr)
    {
        _stdin = stdin;
        _stdout = stdout;
        _stderr = stderr;
    }

    public override void OnLoad(IServiceScope scope)
    {
        base.OnLoad(scope);

        CoreLog.Handler = (level, msg) =>
        {
            _stderr.WriteLine($"[{level}] {msg}");
        };
    }

    public override void OnInitialize()
    {
        _dispatcher = Scope.Resolve<IDispatcher>();
        _bus = Scope.Resolve<IMessageBus>();

        _outputSubscription = _bus.Listen<ConsoleOutputMessage>(HandleOutput);
    }

    public override void OnStart()
    {
        _running = true;
        _inputThread = new Thread(ReadInputLoop)
        {
            IsBackground = true,
            Name = "Flos.Console.Input"
        };
        _inputThread.Start();
    }

    public override void OnShutdown()
    {
        _running = false;
        _outputSubscription?.Dispose();
        _outputSubscription = null;
        CoreLog.Handler = null;
    }

    private void HandleOutput(ConsoleOutputMessage msg)
    {
        _stdout.WriteLine(msg.Line);
    }

    private void ReadInputLoop()
    {
        while (_running)
        {
            string? line;
            try
            {
                line = _stdin.ReadLine();
            }
            catch
            {
                break;
            }

            if (line is null) break;

            var msg = new ConsoleInputMessage(line);
            _dispatcher!.Enqueue(() => _bus!.Publish(msg));
        }
    }
}
