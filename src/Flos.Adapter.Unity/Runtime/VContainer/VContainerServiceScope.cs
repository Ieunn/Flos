#if FLOS_VCONTAINER
using System;
using System.Collections.Generic;
using Flos.Core.Module;
using VContainer;
using VContainer.Unity;

namespace Flos.Adapter.Unity.VContainer
{
    /// <summary>
    /// <see cref="IServiceRegistry"/> backed by VContainer.
    /// Accumulates registrations, then builds a child scope on <see cref="Lock"/>.
    /// </summary>
    public sealed class VContainerServiceScope : IServiceRegistry
    {
        private readonly IObjectResolver _parentResolver;
        private IObjectResolver? _childResolver;
        private readonly List<Action<IContainerBuilder>> _registrations = new List<Action<IContainerBuilder>>();
        private readonly Dictionary<Type, object> _instances = new Dictionary<Type, object>();
        private readonly Dictionary<Type, Func<IServiceRegistry, object>> _factories = new Dictionary<Type, Func<IServiceRegistry, object>>();
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

        public void Register<T>(T instance)
        {
            ThrowIfLocked();
            _instances[typeof(T)] = instance!;
            _registrations.Add(builder => builder.Register<T>(instance));
        }

        public void Register<T>(Func<IServiceRegistry, T> factory)
        {
            ThrowIfLocked();
            var key = typeof(T);
            _factories[key] = scope => factory(scope)!;
            _registrations.Add(builder =>
            {
                builder.Register<object>(
                    c => _factories[key](this),
                    Lifetime.Singleton
                ).As<T>();
            });
        }

        public bool TryRegister<T>(T instance)
        {
            ThrowIfLocked();
            if (HasRegistration(typeof(T))) return false;
            Register(instance);
            return true;
        }

        public bool TryRegister<TInterface, TImpl>() where TImpl : class, TInterface, new()
        {
            ThrowIfLocked();
            if (HasRegistration(typeof(TInterface))) return false;
            Register<TInterface, TImpl>();
            return true;
        }

        public bool TryRegister<T>(Func<IServiceRegistry, T> factory)
        {
            ThrowIfLocked();
            if (HasRegistration(typeof(T))) return false;
            Register(factory);
            return true;
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
            return HasRegistration(typeof(T));
        }

        public bool TryResolve<T>(out T? value)
        {
            if (_instances.TryGetValue(typeof(T), out var instance))
            {
                value = (T)instance;
                return true;
            }

            if (_childResolver != null)
                return _childResolver.TryResolve<T>(out value);

            return _parentResolver.TryResolve<T>(out value);
        }

        public void Dispose()
        {
            if (_childResolver is IDisposable disposable)
                disposable.Dispose();
            _childResolver = null;
        }

        private bool HasRegistration(Type key)
        {
            if (_instances.ContainsKey(key))
                return true;

            if (_factories.ContainsKey(key))
                return true;

            if (_childResolver != null)
            {
                return _childResolver.TryResolve(key, out _);
            }

            return _parentResolver.TryResolve(key, out _);
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
