/// <summary>Small files round-trip through the URL fragment; large ones get no link.</summary>
public class ShareLinkTests
{
    [Test]
    public async Task RoundTrips()
    {
        var document = Document.FromText("my diagram.mmd", "graph TD\n  A[Zürich] --> B", FileTypes.Text);
        var fragment = ShareLink.Encode(document);
        await Assert.That(fragment).IsNotNull();
        await Assert.That(ShareLink.TryDecode("#" + fragment, out var name, out var bytes, out var minified)).IsTrue();
        await Assert.That(name).IsEqualTo("my diagram.mmd");
        await Assert.That(bytes).IsEquivalentTo(document.Bytes);
        await Assert.That(minified).IsFalse();
    }

    [Test]
    public async Task OversizeJsonGoesCompact()
    {
        // Grows an indented file until it no longer fits as it is; compact, it still should.
        string? fragment = null;
        var json = "";
        for (var count = 50; fragment is null || !fragment.EndsWith("&min=1"); count += 50)
        {
            var random = new Random(count);
            var items = Enumerable.Range(0, count).Select(_ => $"        {{\n            \"id\": {random.Next()}\n        }}");
            json = "{\n    \"items\": [\n" + string.Join(",\n", items) + "\n    ]\n}";
            fragment = ShareLink.Encode(Document.FromText("big.json", json, FileTypes.Json));
            await Assert.That(fragment).IsNotNull();
        }

        await Assert.That(ShareLink.TryDecode(fragment, out _, out var bytes, out var minified)).IsTrue();
        await Assert.That(minified).IsTrue();
        await Assert.That(Encoding.UTF8.GetString(bytes)).IsEqualTo(PrettyPrint.Json(json, indent: false));
    }

    [Test]
    public async Task LargeInputHasNoLink()
    {
        var bytes = new byte[20000];
        Random.Shared.NextBytes(bytes);
        await Assert.That(ShareLink.Encode(new("noise.bin", bytes, FileTypes.Text))).IsNull();
    }

    [Test]
    public async Task RejectsGarbage()
    {
        await Assert.That(ShareLink.TryDecode("#name=a.txt&data=!!!", out _, out _, out _)).IsFalse();
        await Assert.That(ShareLink.TryDecode("#other", out _, out _, out _)).IsFalse();
        await Assert.That(ShareLink.TryDecode(null, out _, out _, out _)).IsFalse();
    }
}
