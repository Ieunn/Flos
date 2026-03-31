using System;
using System.Collections.Generic;
using System.Threading;
using Flos.Adapter;
using Flos.Core.Errors;
using Flos.Core.Scheduling;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Flos.Adapter.Unity
{
    /// <summary>
    /// Bridges <see cref="IAssetProvider"/> to Unity Addressables.
    /// Completion callbacks are dispatched to the main thread via <see cref="IDispatcher"/>.
    /// </summary>
    public sealed class UnityAssetBridge : IAssetProvider
    {
        private IDispatcher? _dispatcher;
        private readonly Dictionary<string, AsyncOperationHandle> _handles = new Dictionary<string, AsyncOperationHandle>();

        /// <summary>
        /// Must be called during module initialization to wire the dispatcher.
        /// </summary>
        internal void Initialize(IDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        public void Load<T>(string key, Action<Result<T>> callback, CancellationToken cancellation = default) where T : class
        {
            var handle = Addressables.LoadAssetAsync<T>(key);
            handle.Completed += op =>
            {
                var dispatcher = _dispatcher;
                if (dispatcher == null || cancellation.IsCancellationRequested) return;

                if (op.Status == AsyncOperationStatus.Succeeded)
                {
                    var result = Result<T>.Ok(op.Result);
                    dispatcher.Enqueue(() => { if (!cancellation.IsCancellationRequested) callback(result); });
                }
                else
                {
                    var result = Result<T>.Fail(AdapterErrors.AssetLoadFailed);
                    dispatcher.Enqueue(() => { if (!cancellation.IsCancellationRequested) callback(result); });
                }
            };

            if (_handles.TryGetValue(key, out var existingHandle))
            {
                Addressables.Release(existingHandle);
            }
            _handles[key] = handle;
        }

        public void Release(string key)
        {
            if (_handles.TryGetValue(key, out var handle))
            {
                Addressables.Release(handle);
                _handles.Remove(key);
            }
        }
    }
}
