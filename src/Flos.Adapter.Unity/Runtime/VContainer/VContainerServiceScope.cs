#if FLOS_VCONTAINER
using System;
using System.Collections.Generic;
using Flos.Core.Module;
using VContainer;
using VContainer.Unity;

namespace Flos.Adapter.Unity.VContainer
{
    /// <summary>
    /// <see cref="IServiceScope"/> backed by VContainer.
    /// Accumulates registrations, then builds a child scope on <see cref="Lock"/>.
    /// </summary>
    public sealed class VContainerServiceScope : IServiceScope
    {
        private readonly IObjectResolver _parentResolver;
        private IObjectResolver? _childResolver;
        private readonly List<Action<IContainerBuilder>> _registrations = new List<Action<IContainerBuilder>>();
        private readonly Dictionary<Type, object> _instances = new Dictionary<Type, object>();
        private bool _locked;

        public bool IsLocked => _locked;

        public VContainerServiceScope(IObjectResolver parentResolver)
        {
            _parentResolver = parentResolver;
        }

        public void Register<TInterface, TImpl>() where TImpl : class, TInterface, new()
        {
            ThrowIfLocked();
            _registrations.Add(builder => builder.Register<TImpl>(Lifetime.Singleton).As<TInterface>());
        }

        public void RegisterInstance<T>(T instance)
        {
            ThrowIfLocked();
            _instances[typeof(T)] = instance!;
            _registrations.Add(builder => builder.RegisterInstance<T>(instance));
        }

        public void RegisterFactory<T>(Func<IServiceScope, T> factory)
        {
            ThrowIfLocked();
            _registrations.Add(builder =>
            {
                var value = factory(this);
                builder.RegisterInstance<T>(value);
            });
        }

        public void Lock()
        {
            if (_locked) return;
            _locked = true;

            _childResolver = _parentResolver.CreateScope(builder =>
            {
                foreach (var reg in _registrations)
                    reg(builder);
            });
        }

        public T Resolve<T>()
        {
            if (_childResolver != null)
                return _childResolver.Resolve<T>();

            if (_instances.TryGetValue(typeof(T), out var instance))
                return (T)instance;

            return _parentResolver.Resolve<T>();
        }

        public bool IsRegistered<T>()
        {
            if (_instances.ContainsKey(typeof(T)))
                return true;

            try
            {
                Resolve<T>();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void Dispose()
        {
            if (_childResolver is IDisposable disposable)
                disposable.Dispose();
            _childResolver = null;
        }

        private void ThrowIfLocked()
        {
            if (_locked)
                throw new Flos.Core.Errors.FlosException(
                    Flos.Core.Errors.CoreErrors.ScopeAlreadyLocked);
        }
    }
}
#endif
