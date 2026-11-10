/// <summary>
/// Services a pack registers after its assemblies load. Microsoft.Extensions.DependencyInjection takes
/// no registrations after <c>Build()</c>, and registering a heavy type in <c>Program.cs</c> would load
/// its assembly at startup; <see cref="PackServiceProviderFactory"/> consults this table when the
/// built container has no answer. WebAssembly has a single scope, so scoped is singleton here.
/// </summary>
public static class PackServices
{
    static ConcurrentDictionary<Type, Func<IServiceProvider, object>> factories = new();
    static ConcurrentDictionary<Type, object> instances = new();

    public static void Add<TService>(Func<IServiceProvider, TService> factory)
        where TService : class =>
        factories[typeof(TService)] = factory;

    public static bool Has(Type type) =>
        factories.ContainsKey(type);

    public static object? Resolve(Type type, IServiceProvider provider)
    {
        if (instances.TryGetValue(type, out var instance))
        {
            return instance;
        }

        if (!factories.TryGetValue(type, out var factory))
        {
            return null;
        }

        return instances.GetOrAdd(type, _ => factory(provider));
    }
}
