using Flos.Core.Errors;
using Flos.Core.Logging;
using Flos.Core.Scheduling;

namespace Flos.Adapter.Godot;

/// <summary>
/// Bridges <see cref="IAssetProvider"/> to Godot's <see cref="ResourceLoader"/>.
/// Asset loading uses <c>ResourceLoader.LoadThreadedRequest</c> for thread-safe async loading.
/// Completion callbacks are dispatched to the main thread via <see cref="IDispatcher.Enqueue"/>.
/// </summary>
public sealed class GodotAssetBridge : IAssetProvider
{
    private IDispatcher? _dispatcher;
    private readonly Dictionary<string, Resource> _loaded = new();
    private readonly List<PendingLoad> _pending = new();

    /// <summary>
    /// Must be called during module initialization to wire the dispatcher.
    /// </summary>
    internal void Initialize(IDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public void Load<T>(string key, Action<Result<T>> callback, CancellationToken cancellation = default) where T : class
    {
        var error = ResourceLoader.LoadThreadedRequest(key);
        if (error != Error.Ok)
        {
            CoreLog.Error($"ResourceLoader.LoadThreadedRequest failed for '{key}': {error}");
            _dispatcher?.Enqueue(() => { if (!cancellation.IsCancellationRequested) callback(Result<T>.Fail(AdapterErrors.AssetLoadFailed)); });
            return;
        }

        _pending.Add(new PendingLoad(key, cancellation, result =>
        {
            if (result is T typed)
            {
                _loaded[key] = result;
                callback(Result<T>.Ok(typed));
            }
            else if (result is null)
            {
                callback(Result<T>.Fail(AdapterErrors.AssetNotFound));
            }
            else
            {
                callback(Result<T>.Fail(AdapterErrors.AssetLoadFailed));
            }
        }));
    }

    /// <summary>
    /// Must be called each frame (from the main thread) to poll threaded load status
    /// and dispatch completed callbacks.
    /// </summary>
    internal void Poll()
    {
        for (int i = _pending.Count - 1; i >= 0; i--)
        {
            var pending = _pending[i];

            if (pending.Cancellation.IsCancellationRequested)
            {
                _pending.RemoveAt(i);
                continue;
            }

            var status = ResourceLoader.LoadThreadedGetStatus(pending.Key);

            if (status == ResourceLoader.ThreadLoadStatus.Loaded)
            {
                _pending.RemoveAt(i);
                var resource = ResourceLoader.LoadThreadedGet(pending.Key);
                var cb = pending.Callback;
                _dispatcher?.Enqueue(() => cb(resource));
            }
            else if (status == ResourceLoader.ThreadLoadStatus.Failed ||
                     status == ResourceLoader.ThreadLoadStatus.InvalidResource)
            {
                _pending.RemoveAt(i);
                CoreLog.Error($"ResourceLoader threaded load failed for '{pending.Key}': {status}");
                var cb = pending.Callback;
                _dispatcher?.Enqueue(() => cb(null));
            }
        }
    }

    public void Release(string key)
    {
        _loaded.Remove(key);
    }

    private readonly record struct PendingLoad(string Key, CancellationToken Cancellation, Action<Resource?> Callback);
}
