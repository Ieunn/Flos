using Flos.Core.Errors;

namespace Flos.Adapter;

/// <summary>
/// Async-via-callback asset loading contract.
/// Implementations bridge to engine-native asset systems (e.g. Unity Addressables, Godot ResourceLoader).
/// Completion callbacks are dispatched to the main thread via <c>IDispatcher.Enqueue()</c>.
/// </summary>
public interface IAssetProvider
{
    /// <summary>
    /// Begin loading an asset. <paramref name="callback"/> is invoked on the main thread when complete.
    /// </summary>
    void Load<T>(string key, Action<Result<T>> callback) where T : class;

    /// <summary>
    /// Release a previously loaded asset.
    /// </summary>
    void Release(string key);
}
