# Emf2Svg

> **Disclaimer:** All code in this repository was written by [Claude Code](https://claude.ai/claude-code) (Anthropic's AI coding assistant), with the human author directing the design and verifying the output.

> **⚠️ Experimental — not production-ready.** This library covers only a small subset of the EMF specification (common drawing primitives like lines, arcs, and basic pens). Many record types are silently skipped. The implemented handlers have not been thoroughly tested against a wide range of real-world EMF files. Do not use this in critical or production systems without validating the output against your specific files.

A pure C# library for converting EMF (Enhanced Metafile) to SVG. Built as a port of the
[libemf2svg](https://github.com/kakwa/libemf2svg) C library.

## Installation

```
dotnet add package Emf2Svg
```

## Library Usage

### File path → SVG string

```csharp
string svg = EmfProcessor.ConvertToString("input.emf");
```

### Stream → SVG string

```csharp
using var stream = File.OpenRead("input.emf");
string svg = EmfProcessor.ConvertToString(stream);
```

### File path → file

```csharp
EmfProcessor.Process("input.emf", "output.svg");
```

### Stream → Stream

Useful when piping into another library without touching the disk:

```csharp
using var emfStream = File.OpenRead("input.emf");
using var svgStream = new MemoryStream();
EmfProcessor.Process(emfStream, svgStream);
```

### EMF → SVG → PNG (with Svg.Skia)

```csharp
// dotnet add package Svg.Skia
string svg = EmfProcessor.ConvertToString("input.emf");

using var skSvg = new SKSvg();
skSvg.FromSvg(svg);

var rect = skSvg.Picture.CullRect;
using var bitmap = new SKBitmap((int)rect.Width, (int)rect.Height);
using var canvas = new SKCanvas(bitmap);
canvas.DrawPicture(skSvg.Picture);

using var png = bitmap.Encode(SKEncodedImageFormat.Png, 100);
File.WriteAllBytes("output.png", png.ToArray());
```

## CLI Usage

The `Emf2Svg.Cli` project provides a command-line interface:

```bash
cd Emf2Svg.Cli
dotnet run -- -i input.emf -o output.svg

# List all record types in a file (useful for checking coverage before converting)
dotnet run -- --list-records -i input.emf
```

## Features

- Pure C#, no native dependencies
- Available as a NuGet library (`Emf2Svg`) and a CLI tool (`Emf2Svg.Cli`)
- Produces SVG output visually identical to `libemf2svg`
- Handles the most common EMF drawing primitives:
  - Lines (`LINETO`, `MOVETOEX`)
  - Arcs (`ARC`, `ANGLEARC`)
  - Pens and colors (`CREATEPEN`, `SELECTOBJECT`, `DELETEOBJECT`)
  - Clipping regions (`INTERSECTCLIPRECT`)
  - Device context save/restore (`SAVEDC`, `RESTOREDC`)
  - Coordinate transforms (`SETWINDOWEXTEX`, `SETVIEWPORTEXTEX`, `SETMAPMODE`)

See [CONTRIBUTING.md](CONTRIBUTING.md) for a full table of all EMF record types and their
implementation status.

## Output

The SVG output format matches `libemf2svg` exactly — same path data, same stroke attributes,
same clip regions. Each drawing primitive becomes a `<path>` element with `stroke` and
`fill="none"`.

## Project Structure

```
Emf2Svg/
├── Emf2Svg.sln
├── Emf2Svg.csproj          NuGet library
├── EmfProcessor.cs         Public API + record dispatch loop
├── EmfStructs.cs           Binary structs (PointL, RectL, ColorRef, LogPen, …)
├── DrawingState.cs         GDI device context + object table
├── CoordTransform.cs       point_cal() equivalent — EMF → SVG coordinates
├── SvgWriter.cs            SVG output (path elements, stroke/fill, clip defs)
├── Handlers/
│   ├── ControlHandlers.cs  HEADER, EOF
│   ├── StateHandlers.cs    Map mode, window/viewport extents, SaveDC/RestoreDC
│   ├── ObjectHandlers.cs   Pen creation, object table, stock objects
│   └── DrawingHandlers.cs  Lines, arcs, clipping
└── Emf2Svg.Cli/
    ├── Emf2Svg.Cli.csproj  CLI project (references the library)
    └── Program.cs          CLI entry point
```

## Building

Requires .NET 9.

```bash
dotnet build
```

## Coordinate Transform

EMF coordinates are converted to SVG using the same formula as `libemf2svg`:

```
svgX = ((emfX - windowOrgX) * scalingX + viewPortOrgX) * globalScaling
svgY = ((emfY - windowOrgY) * scalingY + viewPortOrgY) * globalScaling

globalScaling = imgWidth / |rclBounds.right - rclBounds.left|
scalingX = viewPortExX / windowExX   (when both are set)
```

For `U_MM_TEXT` (the most common map mode), `scalingX = scalingY = 1.0`.

## Credits

This project is a C# port of **[libemf2svg](https://github.com/kakwa/libemf2svg)** by
[Pierre-Jean Coudert (kakwa)](https://github.com/kakwa), licensed under the MIT License.

The binary struct definitions and coordinate transform logic are derived from:
- `src/lib/emf2svg_utils.c` — coordinate math, arc rendering helpers
- `src/lib/emf2svg_rec_*.c` — per-record draw handlers
- `src/lib/emf2svg_rec_control.c` — SVG header/footer generation
- `vendor/libuemf/include/uemf.h` — all EMF binary struct layouts
  (from [libuemf](https://github.com/kakwa/libemf2svg/tree/master/vendor/libuemf))

## License

MIT
