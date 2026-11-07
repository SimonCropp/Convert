namespace Convert;

public static class ConvertServiceCollectionExtensions
{
    /// <summary>
    /// Builds the registry from Core's own types plus <paramref name="packs"/> and registers the
    /// session services. Only pack descriptors are touched: no heavy assembly loads here.
    /// </summary>
    public static IServiceCollection AddConvert(this IServiceCollection services, IReadOnlyList<IPack> packs)
    {
        services.AddSingleton(BuildRegistry(packs));
        services.AddScoped<IToolLoader, ToolLoader>();
        services.AddScoped<BrowserInterop>();
        services.AddScoped<ConvertSession>();
        return services;
    }

    public static ConversionRegistry BuildRegistry(IReadOnlyList<IPack> packs)
    {
        var registry = new ConversionRegistry();
        registry.Register(new CorePack());
        foreach (var pack in packs)
        {
            registry.Register(pack);
        }

        return registry;
    }
}
