using Flos.Core.Errors;

namespace Flos.Adapter;

/// <summary>
/// Async-via-callback persistence contract.
/// Implementations bridge to engine-native storage (e.g. Unity persistentDataPath, Godot user://).
/// Completion callbacks are dispatched to the main thread via <c>IDispatcher.Enqueue()</c>.
/// </summary>
public interface ISaveStorage
{
    /// <summary>
    /// Save data to a named slot. Callback receives <see cref="Unit"/> on success.
    /// </summary>
    void Save(string slot, byte[] data, Action<Result<Unit>> callback);

    /// <summary>
    /// Load data from a named slot. Callback receives the byte array on success.
    /// </summary>
    void Load(string slot, Action<Result<byte[]>> callback);

    /// <summary>
    /// Delete a named save slot. Callback receives <see cref="Unit"/> on success.
    /// </summary>
    void Delete(string slot, Action<Result<Unit>> callback);

    /// <summary>
    /// Check if a named save slot exists. Callback receives a boolean result.
    /// </summary>
    void Exists(string slot, Action<Result<bool>> callback);
}
