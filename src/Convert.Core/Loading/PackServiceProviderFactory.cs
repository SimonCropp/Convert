namespace Convert;

/// <summary>
/// Wraps the built container so a component can inject a service a pack registered with
/// <see cref="PackServices"/> after startup. Morph's components <c>@inject MorphInterop</c>; registering
/// it in <c>Program.cs</c> would load Morph.Blazor at boot. Install with
/// <c>builder.ConfigureContainer(new PackServiceProviderFactory())</c>.
/// </summary>
public sealed class PackServiceProviderFactory : IServiceProviderFactory<IServiceCollection>
{
    public IServiceCollection CreateBuilder(IServiceCollection services) => services;

    public IServiceProvider CreateServiceProvider(IServiceCollection services) =>
        new Provider(services.BuildServiceProvider());

    // Root provider. WebAssemblyHost asks it for IServiceScopeFactory and hands the one scope it creates to
    // the renderer, so returning this here is what puts every component injection through Resolve.
    sealed class Provider(ServiceProvider inner) :
        IServiceScopeFactory,
        IKeyedServiceProvider,
        IServiceProviderIsService,
        IDisposable,
        IAsyncDisposable
    {
        public object? GetService(Type serviceType) =>
            Resolve(this, inner, serviceType);

        public IServiceScope CreateScope() =>
            new Scope(inner.GetRequiredService<IServiceScopeFactory>().CreateScope(), this);

        public object? GetKeyedService(Type serviceType, object? serviceKey) =>
            inner.GetKeyedService(serviceType, serviceKey);

        public object GetRequiredKeyedService(Type serviceType, object? serviceKey) =>
            inner.GetRequiredKeyedService(serviceType, serviceKey);

        public bool IsService(Type serviceType) =>
            IsServiceIn(inner, serviceType);

        public void Dispose() => inner.Dispose();

        public ValueTask DisposeAsync() => inner.DisposeAsync();
    }

    sealed class Scope(IServiceScope inner, IServiceScopeFactory root) :
        IServiceScope,
        IKeyedServiceProvider,
        IServiceProviderIsService,
        IAsyncDisposable
    {
        public IServiceProvider ServiceProvider => this;

        public object? GetService(Type serviceType)
        {
            if (serviceType == typeof(IServiceScopeFactory))
            {
                return root;
            }

            return Resolve(this, inner.ServiceProvider, serviceType);
        }

        public object? GetKeyedService(Type serviceType, object? serviceKey) =>
            inner.ServiceProvider.GetKeyedService(serviceType, serviceKey);

        public object GetRequiredKeyedService(Type serviceType, object? serviceKey) =>
            inner.ServiceProvider.GetRequiredKeyedService(serviceType, serviceKey);

        public bool IsService(Type serviceType) =>
            IsServiceIn(inner.ServiceProvider, serviceType);

        public void Dispose() => inner.Dispose();

        public ValueTask DisposeAsync()
        {
            if (inner is IAsyncDisposable asyncDisposable)
            {
                return asyncDisposable.DisposeAsync();
            }

            inner.Dispose();
            return ValueTask.CompletedTask;
        }
    }

    static object? Resolve(IServiceProvider outer, IServiceProvider inner, Type serviceType)
    {
        if (serviceType == typeof(IServiceProvider))
        {
            return outer;
        }

        if (serviceType == typeof(IServiceScopeFactory) &&
            outer is IServiceScopeFactory factory)
        {
            return factory;
        }

        return inner.GetService(serviceType) ??
               PackServices.Resolve(serviceType, outer);
    }

    static bool IsServiceIn(IServiceProvider inner, Type serviceType) =>
        PackServices.Has(serviceType) ||
        (inner.GetService<IServiceProviderIsService>()?.IsService(serviceType) ?? false);
}
