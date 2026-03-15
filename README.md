# Emf2Svg

A pure C# converter for EMF (Enhanced Metafile) to SVG. Built as a port of the
[libemf2svg](https://github.com/kakwa/libemf2svg) C library.

## Usage

```bash
dotnet run -- -i input.emf -o output.svg

# List all record types in a file (useful for checking coverage before converting)
dotnet run -- --list-records -i input.emf
```

Or after publishing:

```bash
emf2svg -i input.emf -o output.svg
```

## Features

- Pure C#, no native dependencies
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
├── Program.cs              CLI entry point
├── EmfProcessor.cs         Record dispatch loop
├── EmfStructs.cs           Binary structs (PointL, RectL, ColorRef, LogPen, …)
├── DrawingState.cs         GDI device context + object table
├── CoordTransform.cs       point_cal() equivalent — EMF → SVG coordinates
├── SvgWriter.cs            SVG output (path elements, stroke/fill, clip defs)
├── Handlers/
│   ├── ControlHandlers.cs  HEADER, EOF
│   ├── StateHandlers.cs    Map mode, window/viewport extents, SaveDC/RestoreDC
│   ├── ObjectHandlers.cs   Pen creation, object table, stock objects
│   └── DrawingHandlers.cs  Lines, arcs, clipping
└── docs/
    └── NOTES.md            Implementation quirks and C-compatibility notes
```

## Building

Requires .NET 9.

```bash
dotnet build
dotnet run -- -i input.emf -o output.svg
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
