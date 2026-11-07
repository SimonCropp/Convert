using Microsoft.JSInterop;
using Morph;

namespace Convert.Morph;

/// <summary>
/// Run once Morph.Blazor has loaded: its components <c>@inject MorphInterop</c>, which only
/// <see cref="PackServices"/> can supply after startup.
/// </summary>
static class MorphServices
{
    public static void Register(IServiceProvider services)
    {
        PackServices.Add(_ => new MorphInterop(_.GetRequiredService<IJSRuntime>()));
        var http = services.GetRequiredService<HttpClient>();
        MorphFonts.Provider ??= () => FontStore.EnsureAsync(http);
    }
}

/// <summary>
/// Where Morph's renders find their fonts. In the browser, <c>FontStore</c> writes the bundled Aptos
/// faces into the in-memory filesystem; a desktop host (the tests) points it at a folder instead.
/// </summary>
public static class MorphFonts
{
    public static Func<Task<string>>? Provider { get; set; }

    internal static Task<string> DirectoryAsync() =>
        Provider?.Invoke() ?? throw new InvalidOperationException("The Morph pack has not been loaded.");
}
