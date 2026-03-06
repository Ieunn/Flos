namespace Flos.Core.State;

/// <summary>
/// Implemented by <see cref="IStateSlice"/> types that support deep cloning.
/// Use the <c>[DeepClone]</c> source generator for automatic implementation.
/// </summary>
public interface IDeepCloneable<out T>
{
    /// <summary>
    /// Creates a deep copy of this instance.
    /// </summary>
    /// <returns>A new instance that is a deep clone of this object.</returns>
    T DeepClone();
}
