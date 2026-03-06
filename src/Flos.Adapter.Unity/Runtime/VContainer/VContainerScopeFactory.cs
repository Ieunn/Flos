#if FLOS_VCONTAINER
using System;
using System.Collections.Generic;
using Flos.Core.Module;
using VContainer;

namespace Flos.Adapter.Unity.VContainer
{
    /// <summary>
    /// <see cref="IScopeFactory"/> wrapping VContainer's <see cref="IObjectResolver"/>.
    /// </summary>
    public sealed class VContainerScopeFactory : IScopeFactory
    {
        private readonly IObjectResolver _resolver;

        public VContainerScopeFactory(IObjectResolver resolver)
        {
            _resolver = resolver;
        }

        public IServiceRegistry CreateRootScope()
        {
            return new VContainerServiceScope(_resolver);
        }
    }
}
#endif
