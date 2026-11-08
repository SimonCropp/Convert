# To do

Gaps left by v1, and decisions that changed from the plan.

## Chaining

Single hop only. Targets not offered:

- md → pdf (md → html → pdf, or md → docx → pdf)
- md → png
- html → docx → pdf
- mermaid → docx
- geo → png → jpeg
- pub preview (pub → pdf, then the browser's PDF viewer)

Options: a manual "convert this result further" (feed `Parts[0]` back as the source), or graph
path-finding with per-hop options.

## PDF

- **No pdf.js.** Browsers display a PDF natively but expose no API to rasterise one, so v1 previews PDF
  in the built-in viewer (an `<iframe>` on a blob URL) and converts PDF → PNG only through OfficeIMO's
  managed renderer. Some mobile browsers have no inline viewer; the preview links to opening it in a tab.
  Revisit pdf.js (Apache-2.0) if a canvas preview or a second PDF → PNG tool is wanted.
- **PDFium** (Morph.PDFium) has no browser-wasm native: bblanchon.PDFium ships win/linux/osx only.
  Revisit with an emscripten build linked through `NativeFileReference` (large `dotnet.native.wasm`).

## Tools

- Morph: `HtmlConverter` / `PdfHtmlConverter` exist in Morph core but not in
  `Morph.Blazor.ConversionService`, so html → png/pdf/md via Morph is not offered.
- Morph: PNG renders all pages in one call; yielding between pages needs `ConversionService.StartPngPages`,
  which is internal.
- OfficeIMO: the other catalogue routes (`OfficeConversionCapabilityCatalog`, 229 routes,
  `BrowserAvailable` flag).
- OfficeIMO: no HarfBuzz shaping (a native archive in OfficeIMO's repo, not a package); complex scripts
  (Arabic, Indic) are not shaped. `WasmBuildNative` is already on, so linking it is possible.
- OfficeIMO: no Noto fallback fonts for CJK, Arabic and symbols; everything maps onto Aptos.
- OfficeIMO 3.6.0 (the published package) has no `*WithEvidence` API and its `ExcelDocumentReader` is
  internal, so the website converter's automatic 50,000-cell worksheet budget is not applied; "Quick
  preview" (250 rows per sheet) is a manual option. The plan's "Reproducible" PDF profile is not offered.
- Shapefile (path-based, multi-file) read/write.
- Skia is stable SkiaSharp 4.155.0 linked with its emscripten 5.0.6 archive (the package's targets pick
  3.1.56, which emscripten 6.0.2 cannot link: `UseSkiaSharpEmscripten5` in Convert.Web swaps it). When a
  SkiaSharp release selects a 6.x archive itself (4.156 does), drop that target.
- Before upgrading to 4.156: its release candidate crashes drawing a lazily decoded SKImage on the PDF
  canvas, [mono/SkiaSharp#5645](https://github.com/mono/SkiaSharp/issues/5645). Image → PDF routes
  would need to decode to pixels first (`ToRasterImage(true)`) until that is fixed.
- Skia's native code is linked into dotnet.native.wasm, so it is paid at boot, not lazily: 1.62 MB →
  3.05 MB brotli (6.0 → 11.5 MB raw). The managed assemblies stay lazy.
- After changing Skia versions, clear Convert.Web's Debug output: an incremental `dotnet run` build can
  serve the previous managed SkiaSharp.wasm with the new native ("native libSkiaSharp library (155.0) is
  incompatible").
- Skia decodes only the first frame of an animated GIF; ImageSharp keeps the frames.
- HEIF, AVIF and RAW (DNG) are not offered: SkiaSharp's wasm archive links none of those codecs (no
  SkHeifCodec, SkAvifCodec or SkRawCodec in libSkiaSharp.a), only PNG, JPEG, WebP, GIF, BMP, ICO and WBMP.
- Data: CSV and TSV read flat (a dotted header such as orbit.au stays one key, though writing nested
  data produces one). XML keeps no types, so XML to JSON infers numbers and true/false (an option).
- Data: the table preview wins for CSV, so a CSV of coordinates previews as a table, not GeoConvert's map.

## Runtime

- Run conversions in a Web Worker (OfficeIMO's `engine-worker.js` pattern) so long conversions don't
  block the UI thread.
- Per-assembly AOT, once startup size is measured. The interpreted publish is ~98 MB uncompressed on
  disk for everything, almost all of it lazy.
- A test that the per-entry closure also holds inside runner methods: Mono resolves every local of a
  method when it first interprets it, so a runner whose one `switch` holds locals from several routes
  loads them all (found and fixed in OfficeIMO's runners by the browser tests, which the desktop closure
  test cannot see).

## Samples and assets

- Geo ships a small hand-written GeoJSON sample rather than GeoConvert.Web's 7.6 MB OSM borders
  (MapBundle.World is a large package and the data is ODbL).
- Morph's Aptos fonts are not preloaded by index.html (940 KB that only Office files need); they are
  fetched when Morph or OfficeIMO first renders.
- Per-project READMEs.
