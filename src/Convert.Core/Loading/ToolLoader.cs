/// <summary>
/// The browser's <see cref="IToolLoader"/>, over <see cref="LazyAssemblyLoader"/>. That loader throws
/// outside the browser, so tests stub <see cref="IToolLoader"/>.
/// </summary>
public sealed class ToolLoader(LazyAssemblyLoader loader, IServiceProvider services) : IToolLoader
{
    Dictionary<string, Task> loads = new(StringComparer.Ordinal);
    HashSet<string> pending = new(StringComparer.Ordinal);
    HashSet<IPack> loadedPacks = [];

    public IReadOnlyCollection<string> Pending => pending;

    public event Action? Changed;

    public async Task EnsureAsync(IReadOnlyList<string> assemblies, IPack? pack)
    {
        // A failed load is forgotten so a later attempt retries rather than replaying the fault.
        foreach (var name in assemblies)
        {
            if (loads.TryGetValue(name, out var existing) && existing is { IsCompleted: true, IsCompletedSuccessfully: false })
            {
                loads.Remove(name);
            }
        }

        var missing = assemblies.Where(_ => !loads.ContainsKey(_)).Distinct(StringComparer.Ordinal).ToList();
        if (missing.Count > 0)
        {
            var task = LoadAsync(missing);
            foreach (var name in missing)
            {
                loads[name] = task;
            }
        }

        await Task.WhenAll(assemblies.Select(_ => loads[_]));

        if (pack is not null && loadedPacks.Add(pack))
        {
            pack.Loaded(services);
        }
    }

    async Task LoadAsync(List<string> names)
    {
        pending.UnionWith(names);
        Changed?.Invoke();
        try
        {
            await loader.LoadAssembliesAsync(names);
        }
        finally
        {
            pending.ExceptWith(names);
            Changed?.Invoke();
        }
    }
}
