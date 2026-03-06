using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Flos.Adapter;
using Flos.Core.Errors;
using Flos.Core.Logging;
using Flos.Core.Scheduling;
using UnityEngine;

namespace Flos.Adapter.Unity
{
    /// <summary>
    /// Bridges <see cref="ISaveStorage"/> to Unity's <see cref="Application.persistentDataPath"/>.
    /// File I/O runs on a background thread; callbacks are dispatched to the main thread
    /// via <see cref="IDispatcher.Enqueue"/>.
    /// </summary>
    public sealed class UnitySaveBridge : ISaveStorage
    {
        private IDispatcher? _dispatcher;
        private readonly string _basePath;

        public UnitySaveBridge()
        {
            _basePath = Path.Combine(Application.persistentDataPath, "saves");
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
                    var dir = Path.GetDirectoryName(path);
                    if (dir != null)
                        Directory.CreateDirectory(dir);
                    using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
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
                    if (!File.Exists(path))
                    {
                        if (!cancellation.IsCancellationRequested)
                            dispatcher.Enqueue(() => callback(Result<byte[]>.Fail(AdapterErrors.SlotNotFound)));
                        return;
                    }
                    var data = File.ReadAllBytes(path);
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
                    if (File.Exists(path))
                        File.Delete(path);
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
                    var exists = File.Exists(path);
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

        private string SlotPath(string slot) => Path.Combine(_basePath, slot + ".sav");
    }
}
