namespace Convert;

/// <summary>
/// Core's JavaScript surface: <c>_content/Convert.Core/convert.js</c>, imported as an ES module on first
/// use. <c>byte[]</c> crosses to JavaScript as a <c>Uint8Array</c>.
/// </summary>
public sealed class BrowserInterop(IJSRuntime runtime) : IAsyncDisposable
{
    public const string ContentRoot = "_content/Convert.Core";

    Task<IJSObjectReference>? import;

    Task<IJSObjectReference> Module =>
        import ??= runtime.InvokeAsync<IJSObjectReference>("import", $"./{ContentRoot}/convert.js").AsTask();

    /// <summary>Hands bytes to the browser as a file download.</summary>
    public async Task DownloadAsync(string fileName, string contentType, byte[] bytes)
    {
        var module = await Module;
        await module.InvokeVoidAsync("download", fileName, contentType, bytes);
    }

    /// <summary>Wraps bytes in a blob URL. The caller must <see cref="RevokeObjectUrlAsync"/> it.</summary>
    public async Task<string> CreateObjectUrlAsync(string contentType, byte[] bytes)
    {
        var module = await Module;
        return await module.InvokeAsync<string>("createObjectUrl", contentType, bytes);
    }

    public async Task RevokeObjectUrlAsync(string url)
    {
        var module = await Module;
        await module.InvokeVoidAsync("revokeObjectUrl", url);
    }

    /// <summary>Routes a file dropped anywhere on the page into <paramref name="input"/>, as if picked there.</summary>
    public async Task AcceptDropsAsync(ElementReference input)
    {
        var module = await Module;
        await module.InvokeVoidAsync("acceptDrops", input);
    }

    public async Task<string?> UserAgentAsync()
    {
        var module = await Module;
        return await module.InvokeAsync<string?>("userAgent");
    }

    public async ValueTask DisposeAsync()
    {
        if (import is null)
        {
            return;
        }

        try
        {
            var module = await import;
            await module.DisposeAsync();
        }
        catch (JSDisconnectedException)
        {
        }
    }
}
