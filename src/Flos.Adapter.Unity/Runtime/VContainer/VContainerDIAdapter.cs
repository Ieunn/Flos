#if FLOS_VCONTAINER
using System;
using System.Collections.Generic;
using Flos.Core.Module;
using VContainer;

namespace Flos.Adapter.Unity.VContainer
{
    /// <summary>
    /// <see cref="IDIAdapter"/> wrapping VContainer's <see cref="IObjectResolver"/>.
    /// </summary>
    public sealed class VContainerDIAdapter : IDIAdapter
    {
        private readonly IObjectResolver _resolver;

        public VContainerDIAdapter(IObjectResolver resolver)
        {
            _resolver = resolver;
        }

        public IServiceScope CreateRootScope()
        {
            return new VContainerServiceScope(_resolver);
        }
    }
}
#endif
