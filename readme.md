# Convert

https://simoncropp.github.io/Convert/

Convert and render files entirely in the browser. Drop a file, preview it, pick a target format,
pick a tool when several can do it, set its options, preview the result and download it. Nothing leaves
the device: every conversion runs in .NET on WebAssembly.

## Formats

| Source | Targets | Tools |
|---|---|---|
| Word, Excel, PowerPoint | PDF, PNG, HTML, Markdown, text | Morph, OfficeIMO (PDF) |
| Publisher | PDF | OfficeIMO |
| PDF | Word, Excel, PowerPoint, HTML, PNG, Markdown, text | OfficeIMO |
| HTML | PDF, Word, Markdown | OfficeIMO, OpenXmlHtml |
| Markdown | HTML, Word, PDF | Markdig, OfficeIMO |
| Text | PDF, Word | OfficeIMO |
| Mermaid | SVG, PNG, PDF | Naiad (PDF drawn by Skia) |
| GeoJSON, TopoJSON, KML, KMZ, GPX, WKT, WKB, CSV (with geometry), FlatGeobuf, GeoParquet | each other, PNG, SVG | GeoConvert |
| SVG, PNG, JPEG, WebP | PNG, JPEG, WebP | the browser's canvas |
| PNG, JPEG, WebP, GIF, BMP, TIFF, ICO, CUR, ANI, TGA, QOI, Netpbm, OpenEXR | each other | ImageSharp |
| PNG, JPEG, WebP, GIF, BMP, ICO, WBMP | PNG, JPEG, WebP, PDF | Skia |
| SVG | PNG, JPEG, WebP, PDF, ICO | Skia (Svg.Skia) |
| SVG, PNG, JPEG, WebP, GIF, BMP, ICO, WBMP | Favicon set (ICO, Apple touch and manifest PNGs, webmanifest, HTML) | Skia |
| JSON, YAML, TOML, XML, CSV, TSV | each other | .NET, YamlDotNet, Tomlyn |
| CSV, TSV | Excel | OfficeIMO |
| Excel | CSV, TSV (one per worksheet) | OfficeIMO |
| SVG, PNG, JPEG, WebP, GIF, BMP, ICO | Data URI | .NET |

The full list, with each route's lazy assemblies, is [docs/catalogue.md](docs/catalogue.md) (generated
by a test). Text formats can be pasted and edited with a live preview; Word documents and Excel workbooks can be
edited in Morph's editor.

## Architecture

- `src/Convert.Core`: the model (`FileType`, `Conversion`, `Preview`, `Editor`, `IPack`), the registry,
  lazy loading (`ToolLoader`) and late-binding DI (`PackServices`), the session and the wizard's
  components.
- `src/Convert.<Tool>`: one pack per tool. A pack's own assembly is small and loads at startup so the
  registry exists; the tool's libraries are `BlazorWebAssemblyLazyLoad` and download the first time an
  entry needs them. Only `Runners/`, `Previews/` and `Editors/` may name a tool's namespace.
- `src/Convert.Web`: the app shell and the one wizard page; owns the lazy-load list.
- `src/Convert.Tests`: TUnit + Verify + bUnit + Playwright. Guards the lazy-load rules (each entry
  declares its whole closure, the csproj list is exactly the union, startup types name no lazy assembly),
  runs every conversion on desktop, snapshots the option panels, and drives the published app in Chromium.

Known gaps: [docs/todo.md](docs/todo.md).

## Build and run

Needs the SDK in [global.json](global.json) and the wasm-tools workload (Krilla's PDF native is linked
into `dotnet.native.wasm`):

```bash
dotnet workload install wasm-tools
```

```bash
dotnet run --project src/Convert.Web
```

Tests (the build also publishes the app for the browser tests):

```bash
dotnet build src/Convert.Tests -c Release
```

```bash
dotnet src/Convert.Tests/bin/Release/net11.0/Convert.Tests.dll
```
