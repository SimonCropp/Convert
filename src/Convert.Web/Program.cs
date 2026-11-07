var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Lets a pack register services once its assemblies load (Morph's MorphInterop), without naming them here.
builder.ConfigureContainer(new PackServiceProviderFactory());

builder.Services.AddScoped(_ =>
    new HttpClient
    {
        BaseAddress = new(builder.HostEnvironment.BaseAddress)
    });
builder.Services.AddConvert(Packs.All);
builder.Services.AddScoped<ThemePreferenceService>();

await builder.Build().RunAsync();
