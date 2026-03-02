using System;
using System.IO;
using System.Threading.Tasks;
using Flos.Adapter;
using Flos.Core.Errors;
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

        public void Save(string slot, byte[] data, Action<Result<Unit>> callback)
        {
            var path = SlotPath(slot);
            var dispatcher = _dispatcher!;
            Task.Run(() =>
            {
                try
                {
                    var dir = Path.GetDirectoryName(path);
                    if (dir != null)
                        Directory.CreateDirectory(dir);
                    File.WriteAllBytes(path, data);
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
                    if (!File.Exists(path))
                    {
                        dispatcher.Enqueue(() => callback(Result<byte[]>.Fail(AdapterErrors.SlotNotFound)));
                        return;
                    }
                    var data = File.ReadAllBytes(path);
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
                    if (File.Exists(path))
                        File.Delete(path);
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
                    var exists = File.Exists(path);
                    dispatcher.Enqueue(() => callback(Result<bool>.Ok(exists)));
                }
                catch (Exception)
                {
                    dispatcher.Enqueue(() => callback(Result<bool>.Fail(AdapterErrors.LoadFailed)));
                }
            });
        }

        private string SlotPath(string slot) => Path.Combine(_basePath, slot + ".sav");
    }
}
