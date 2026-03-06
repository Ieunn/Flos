using Flos.Core.Errors;
using Flos.Core.Logging;

namespace Flos.Core.Module;

/// <summary>
/// Utility for sorting modules by dependency order and validating pattern requirements.
/// </summary>
public static class ModuleLoader
{
    /// <summary>
    /// Topological sort of modules by their <see cref="IModule.Dependencies"/>.
    /// </summary>
    /// <param name="modules">The modules to sort.</param>
    /// <returns>A new list of modules in dependency order (dependencies before dependents).</returns>
    /// <exception cref="FlosException">Thrown with <see cref="CoreErrors.CircularDependency"/> when a circular dependency is detected.</exception>
    /// <exception cref="FlosException">Thrown with <see cref="CoreErrors.MissingDependency"/> when a declared dependency is not in the module list.</exception>
    public static IReadOnlyList<IModule> TopologicalSort(IReadOnlyList<IModule> modules)
    {
        var byId = new Dictionary<string, IModule>(modules.Count);
        foreach (var m in modules)
        {
            if (!byId.TryAdd(m.Id, m))
            {
                throw new FlosException(CoreErrors.InitializationFailed,
                    $"Duplicate module ID '{m.Id}'. Each module must have a unique Id.");
            }
        }

        var sorted = new List<IModule>(modules.Count);
        var visited = new Dictionary<string, VisitState>(modules.Count);
        var path = new List<string>();

        foreach (var m in modules)
        {
            Visit(m, byId, visited, sorted, path);
        }

        return sorted;
    }

    private enum VisitState { InProgress, Done }

    private static void Visit(
        IModule module,
        Dictionary<string, IModule> byId,
        Dictionary<string, VisitState> visited,
        List<IModule> sorted,
        List<string> path)
    {
        if (visited.TryGetValue(module.Id, out var state))
        {
            if (state == VisitState.Done) return;
            path.Add(module.Id);
            var cycleStart = path.IndexOf(module.Id);
            var cycle = string.Join(" → ", path.Skip(cycleStart));
            throw new FlosException(CoreErrors.CircularDependency, $"Circular dependency detected: {cycle}");
        }

        visited[module.Id] = VisitState.InProgress;
        path.Add(module.Id);

        foreach (var depId in module.Dependencies)
        {
            if (!byId.TryGetValue(depId, out var dep))
            {
                throw new FlosException(CoreErrors.MissingDependency,
                    $"Module '{module.Id}' depends on '{depId}', which is not in the module list.");
            }
            Visit(dep, byId, visited, sorted, path);
        }

        path.RemoveAt(path.Count - 1);
        visited[module.Id] = VisitState.Done;
        sorted.Add(module);
    }

    /// <summary>
    /// Validates that all modules' <see cref="IModule.RequiredPatterns"/> are satisfied by loaded patterns.
    /// </summary>
    /// <param name="modules">The modules whose pattern requirements to validate.</param>
    /// <param name="registry">The pattern registry to check against.</param>
    /// <exception cref="FlosException">Thrown with <see cref="CoreErrors.MissingPattern"/> when a required pattern is not loaded.</exception>
    public static void ValidatePatterns(IReadOnlyList<IModule> modules, IPatternRegistry registry)
    {
        foreach (var module in modules)
        {
            foreach (var required in module.RequiredPatterns)
            {
                if (!registry.IsLoaded(required))
                {
                    throw new FlosException(CoreErrors.MissingPattern,
                        $"Module '{module.Id}' requires pattern '{required.Name}', which is not loaded.");
                }
            }
        }
    }
}
