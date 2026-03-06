using System;
using System.Collections.Generic;
using Flos.Core.Module;
using Flos.Core.Scheduling;
using Flos.Core.Sessions;
using UnityEngine;

namespace Flos.Adapter.Unity
{
    /// <summary>
    /// MonoBehaviour that owns and drives an <see cref="ISession"/>.
    /// Attach to a GameObject to bootstrap Flos in Unity.
    /// Subclass and override <see cref="GetModules"/> to add game modules.
    /// </summary>
    public abstract class FlosSession : MonoBehaviour
    {
        [Header("Scheduling")]
        [SerializeField] private TickMode _tickMode = TickMode.FixedTick;
        [SerializeField] private float _fixedTimeStep = 1f / 60f;

        [Header("Lifecycle")]
        [SerializeField] private bool _autoInitialize = true;

        private ISession? _session;
        private bool _started;

        /// <summary>The Flos session. Null until <see cref="Initialize"/> is called.</summary>
        public ISession? Session => _session;

        /// <summary>
        /// Optional scope factory. Set before <see cref="Initialize"/> to use a custom DI container.
        /// </summary>
        public IScopeFactory? ScopeFactory { get; set; }

        protected virtual void Awake()
        {
            if (_autoInitialize)
                Initialize();
        }

        protected virtual void Start()
        {
            if (_autoInitialize && _session != null)
                StartSession();
        }

        protected virtual void FixedUpdate()
        {
            if (_tickMode == TickMode.FixedTick && _session != null && _session.State == SessionState.Running)
            {
                _session.Scheduler.Tick(Time.fixedDeltaTime);
            }
        }

        /// <summary>
        /// Advance one tick manually. Only meaningful when <see cref="TickMode"/> is <see cref="TickMode.StepBased"/>.
        /// </summary>
        public void Step()
        {
            if (_tickMode == TickMode.StepBased && _session != null && _session.State == SessionState.Running)
            {
                _session.Scheduler.Step();
            }
        }

        protected virtual void OnApplicationPause(bool pauseStatus)
        {
            if (_session == null) return;

            if (pauseStatus && _session.State == SessionState.Running)
                _session.Pause();
            else if (!pauseStatus && _session.State == SessionState.Paused)
                _session.Resume();
        }

        protected virtual void OnDestroy()
        {
            if (_session != null)
            {
                if (_session.State == SessionState.Running || _session.State == SessionState.Paused)
                    _session.Shutdown();
                _session.Dispose();
                _session = null;
            }
        }

        /// <summary>
        /// Create and initialize the session. Called automatically if <c>autoInitialize</c> is true.
        /// </summary>
        public void Initialize()
        {
            if (_session != null) return;

            _session = new Session();
            _session.Initialize(new SessionConfig
            {
                Modules = GetModules(),
                TickMode = _tickMode,
                FixedTimeStep = _fixedTimeStep,
                ScopeFactory = ScopeFactory,
            });
        }

        /// <summary>
        /// Start the session. Called automatically after <see cref="Initialize"/> if <c>autoInitialize</c> is true.
        /// </summary>
        public void StartSession()
        {
            if (_started || _session == null) return;
            _started = true;
            _session.Start();
        }

        /// <summary>
        /// Override to provide game modules. Must include <see cref="UnityAdapterModule"/> and any game-specific modules.
        /// </summary>
        protected abstract IReadOnlyList<IModule> GetModules();
    }
}
