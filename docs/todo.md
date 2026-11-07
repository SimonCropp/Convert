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
- Skia backends: no browser-wasm natives.

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
