using Flos.Core.Errors;
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
        _basePath = System.IO.Path.Combine(
            ProjectSettings.GlobalizePath("user://saves/"));
    }

    /// <summary>
    /// Must be called during module initialization to wire the dispatcher.
    /// </summary>
    internal void Initialize(IDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public void Save(string slot, byte[] data, Action<Result<Unit>> callback)
    {
        var path = SlotPath(slot);
        var dispatcher = _dispatcher!;
        Task.Run(() =>
        {
            try
            {
                var dir = System.IO.Path.GetDirectoryName(path);
                if (dir != null)
                    System.IO.Directory.CreateDirectory(dir);
                System.IO.File.WriteAllBytes(path, data);
                dispatcher.Enqueue(() => callback(Result<Unit>.Ok(Unit.Value)));
            }
            catch (Exception)
            {
                dispatcher.Enqueue(() => callback(Result<Unit>.Fail(AdapterErrors.SaveFailed)));
            }
        });
    }

    public void Load(string slot, Action<Result<byte[]>> callback)
    {
        var path = SlotPath(slot);
        var dispatcher = _dispatcher!;
        Task.Run(() =>
        {
            try
            {
                if (!System.IO.File.Exists(path))
                {
                    dispatcher.Enqueue(() => callback(Result<byte[]>.Fail(AdapterErrors.SlotNotFound)));
                    return;
                }
                var data = System.IO.File.ReadAllBytes(path);
                dispatcher.Enqueue(() => callback(Result<byte[]>.Ok(data)));
            }
            catch (Exception)
            {
                dispatcher.Enqueue(() => callback(Result<byte[]>.Fail(AdapterErrors.LoadFailed)));
            }
        });
    }

    public void Delete(string slot, Action<Result<Unit>> callback)
    {
        var path = SlotPath(slot);
        var dispatcher = _dispatcher!;
        Task.Run(() =>
        {
            try
            {
                if (System.IO.File.Exists(path))
                    System.IO.File.Delete(path);
                dispatcher.Enqueue(() => callback(Result<Unit>.Ok(Unit.Value)));
            }
            catch (Exception)
            {
                dispatcher.Enqueue(() => callback(Result<Unit>.Fail(AdapterErrors.DeleteFailed)));
            }
        });
    }

    public void Exists(string slot, Action<Result<bool>> callback)
    {
        var path = SlotPath(slot);
        var dispatcher = _dispatcher!;
        Task.Run(() =>
        {
            try
            {
                var exists = System.IO.File.Exists(path);
                dispatcher.Enqueue(() => callback(Result<bool>.Ok(exists)));
            }
            catch (Exception)
            {
                dispatcher.Enqueue(() => callback(Result<bool>.Fail(AdapterErrors.LoadFailed)));
            }
        });
    }

    private string SlotPath(string slot) => System.IO.Path.Combine(_basePath, slot + ".sav");
}
