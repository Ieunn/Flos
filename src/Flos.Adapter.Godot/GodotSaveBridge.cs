using Flos.Core.Errors;
using Flos.Core.Logging;
using Flos.Core.Scheduling;

namespace Flos.Adapter.Godot;

/// <summary>
/// Bridges <see cref="ISaveStorage"/> to Godot's <c>user://saves/</c> directory.
/// File I/O runs on a background thread; callbacks are dispatched to the main thread
/// via <see cref="IDispatcher.Enqueue"/>.
/// </summary>
public sealed class GodotSaveBridge : ISaveStorage
{
    private IDispatcher? _dispatcher;
    private readonly string _basePath;

    public GodotSaveBridge()
    {
        _basePath = ProjectSettings.GlobalizePath("user://saves/");
    }

    /// <summary>
    /// Must be called during module initialization to wire the dispatcher.
    /// </summary>
    internal void Initialize(IDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public void Save(string slot, ReadOnlyMemory<byte> data, Action<Result<Unit>> callback, CancellationToken cancellation = default)
    {
        var path = SlotPath(slot);
        var dispatcher = _dispatcher!;
        Task.Run(() =>
        {
            if (cancellation.IsCancellationRequested) return;
            try
            {
                var dir = System.IO.Path.GetDirectoryName(path);
                if (dir != null)
                    System.IO.Directory.CreateDirectory(dir);
                using var fs = new System.IO.FileStream(path, System.IO.FileMode.Create, System.IO.FileAccess.Write, System.IO.FileShare.None);
                fs.Write(data.Span);
                if (!cancellation.IsCancellationRequested)
                    dispatcher.Enqueue(() => callback(Result<Unit>.Ok(Unit.Value)));
            }
            catch (Exception ex)
            {
                CoreLog.Error($"Save failed for slot '{slot}': {ex.Message}");
                if (!cancellation.IsCancellationRequested)
                    dispatcher.Enqueue(() => callback(Result<Unit>.Fail(AdapterErrors.SaveFailed)));
            }
        }, cancellation);
    }

    public void Load(string slot, Action<Result<byte[]>> callback, CancellationToken cancellation = default)
    {
        var path = SlotPath(slot);
        var dispatcher = _dispatcher!;
        Task.Run(() =>
        {
            if (cancellation.IsCancellationRequested) return;
            try
            {
                if (!System.IO.File.Exists(path))
                {
                    if (!cancellation.IsCancellationRequested)
                        dispatcher.Enqueue(() => callback(Result<byte[]>.Fail(AdapterErrors.SlotNotFound)));
                    return;
                }
                var data = System.IO.File.ReadAllBytes(path);
                if (!cancellation.IsCancellationRequested)
                    dispatcher.Enqueue(() => callback(Result<byte[]>.Ok(data)));
            }
            catch (Exception ex)
            {
                CoreLog.Error($"Load failed for slot '{slot}': {ex.Message}");
                if (!cancellation.IsCancellationRequested)
                    dispatcher.Enqueue(() => callback(Result<byte[]>.Fail(AdapterErrors.LoadFailed)));
            }
        }, cancellation);
    }

    public void Delete(string slot, Action<Result<Unit>> callback, CancellationToken cancellation = default)
    {
        var path = SlotPath(slot);
        var dispatcher = _dispatcher!;
        Task.Run(() =>
        {
            if (cancellation.IsCancellationRequested) return;
            try
            {
                if (System.IO.File.Exists(path))
                    System.IO.File.Delete(path);
                if (!cancellation.IsCancellationRequested)
                    dispatcher.Enqueue(() => callback(Result<Unit>.Ok(Unit.Value)));
            }
            catch (Exception ex)
            {
                CoreLog.Error($"Delete failed for slot '{slot}': {ex.Message}");
                if (!cancellation.IsCancellationRequested)
                    dispatcher.Enqueue(() => callback(Result<Unit>.Fail(AdapterErrors.DeleteFailed)));
            }
        }, cancellation);
    }

    public void Exists(string slot, Action<Result<bool>> callback, CancellationToken cancellation = default)
    {
        var path = SlotPath(slot);
        var dispatcher = _dispatcher!;
        Task.Run(() =>
        {
            if (cancellation.IsCancellationRequested) return;
            try
            {
                var exists = System.IO.File.Exists(path);
                if (!cancellation.IsCancellationRequested)
                    dispatcher.Enqueue(() => callback(Result<bool>.Ok(exists)));
            }
            catch (Exception ex)
            {
                CoreLog.Error($"Exists check failed for slot '{slot}': {ex.Message}");
                if (!cancellation.IsCancellationRequested)
                    dispatcher.Enqueue(() => callback(Result<bool>.Fail(AdapterErrors.LoadFailed)));
            }
        }, cancellation);
    }

    private string SlotPath(string slot) => System.IO.Path.Combine(_basePath, slot + ".sav");
}
