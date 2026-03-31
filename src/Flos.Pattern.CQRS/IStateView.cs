using Flos.Core.State;

namespace Flos.Pattern.CQRS;

/// <summary>
/// Read-only point-in-time snapshot of the world state.
/// Extends <see cref="IStateReader"/> — code that needs read-only state access
/// should depend on <see cref="IStateReader"/> rather than on this type directly.
/// </summary>
public interface IStateView : IStateReader
{
}
