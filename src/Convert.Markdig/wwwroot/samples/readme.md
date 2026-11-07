# Convert

Convert, edit and render files **entirely in the browser**. Nothing you open leaves your device.

## What it does

1. Drop a file, or paste some text.
2. Pick a format to convert to.
3. Pick a tool when more than one can do it, and set its options.
4. Preview the result and download it.

## Formats

| Source | Targets |
|---|---|
| Word, Excel, PowerPoint | PDF, PNG, HTML, Markdown, text |
| Markdown | HTML, Word |
| Mermaid | SVG, PNG |
| GeoJSON, KML, GPX, … | each other, PNG, SVG |

> Every conversion runs in WebAssembly, on your machine.

- [x] Single-hop conversions
- [ ] Chained conversions

```csharp
var html = Markdown.ToHtml("# Hello");
```
