using Flos.Core.Errors;
using Flos.Core.Scheduling;

namespace Flos.Adapter.Godot;

/// <summary>
/// Bridges <see cref="IAssetProvider"/> to Godot's <see cref="ResourceLoader"/>.
/// Synchronous <c>ResourceLoader.Load</c> is called on a background thread via <see cref="Task.Run"/>;
/// the result is dispatched to the main thread via <see cref="IDispatcher.Enqueue"/>.
/// </summary>
public sealed class GodotAssetBridge : IAssetProvider
{
    private IDispatcher? _dispatcher;
    private readonly Dictionary<string, Resource> _loaded = new();

    /// <summary>
    /// Must be called during module initialization to wire the dispatcher.
    /// </summary>
    internal void Initialize(IDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public void Load<T>(string key, Action<Result<T>> callback) where T : class
    {
        var dispatcher = _dispatcher!;
        Task.Run(() =>
        {
            try
            {
                var resource = ResourceLoader.Load(key);
                if (resource == null)
                {
                    dispatcher.Enqueue(() => callback(Result<T>.Fail(AdapterErrors.AssetNotFound)));
                    return;
                }

                if (resource is T typed)
                {
                    lock (_loaded)
                    {
                        _loaded[key] = resource;
                    }
                    dispatcher.Enqueue(() => callback(Result<T>.Ok(typed)));
                }
                else
                {
                    dispatcher.Enqueue(() => callback(Result<T>.Fail(AdapterErrors.AssetLoadFailed)));
                }
            }
            catch (Exception)
            {
                dispatcher.Enqueue(() => callback(Result<T>.Fail(AdapterErrors.AssetLoadFailed)));
            }
        });
    }

    public void Release(string key)
    {
        lock (_loaded)
        {
            _loaded.Remove(key);
        }
    }
}
