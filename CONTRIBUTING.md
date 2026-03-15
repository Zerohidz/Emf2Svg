# Contributing

Emf2Svg is a C# port of [libemf2svg](https://github.com/kakwa/libemf2svg). Contributions
that expand EMF record coverage are very welcome.

## Architecture

Each EMF record type maps to a single handler method. Adding support for a new record type
is generally a three-step process:

1. Add the record type constant to `EmfProcessor.cs` (`EmfRecordType`).
2. Write a handler method in the appropriate file under `Handlers/`.
3. Add a `case` in the `EmfProcessor.Process` switch statement.

The coordinate transform (`CoordTransform.TransformPoint`) and SVG writer (`SvgWriter`)
already handle the heavy lifting — most new handlers just need to parse the binary payload
and call the right writer method.

For the binary layout of any record, the authoritative reference is
[`vendor/libuemf/include/uemf.h`](https://github.com/kakwa/libemf2svg/blob/master/vendor/libuemf/include/uemf.h)
in the libemf2svg repository.

---

## TODO — Record Coverage

Records are grouped by implementation difficulty. The 19 types currently handled are marked ✅.

### Shapes & Primitives

| Record | Type ID | Status | Notes |
|--------|---------|--------|-------|
| `LINETO` | 54 | ✅ | |
| `MOVETOEX` | 27 | ✅ | |
| `ARC` | 45 | ✅ | |
| `ANGLEARC` | 41 | ✅ | |
| `RECTANGLE` | 43 | ⬜ | `<rect>` |
| `ELLIPSE` | 42 | ⬜ | `<ellipse>` |
| `ROUNDRECT` | 44 | ⬜ | `<rect rx ry>` |
| `PIE` | 56 | ⬜ | Arc + lines to center |
| `CHORD` | 46 | ⬜ | Arc + closing line |
| `ARCTO` | 55 | ⬜ | Same as ARC but updates current pos |
| `SETPIXELV` | 15 | ⬜ | Single pixel — can ignore |

### Polyline / Polygon Families

| Record | Type ID | Status | Notes |
|--------|---------|--------|-------|
| `POLYLINE` | 4 | ⬜ | `<path d="M L L ...">` |
| `POLYLINE16` | 87 | ⬜ | 16-bit coordinate variant |
| `POLYLINETO` | 6 | ⬜ | Continues from current pos |
| `POLYLINETO16` | 88 | ⬜ | 16-bit variant |
| `POLYGON` | 3 | ⬜ | Closed polyline |
| `POLYGON16` | 86 | ⬜ | 16-bit variant |
| `POLYPOLYGON` | 8 | ⬜ | Multiple polygons |
| `POLYPOLYGON16` | 91 | ⬜ | 16-bit variant |
| `POLYPOLYLINE` | 7 | ⬜ | Multiple polylines |
| `POLYPOLYLINE16` | 90 | ⬜ | 16-bit variant |
| `POLYBEZIER` | 2 | ⬜ | `<path d="M C C ...">` |
| `POLYBEZIER16` | 85 | ⬜ | 16-bit variant |
| `POLYBEZIERTO` | 5 | ⬜ | Continues from current pos |
| `POLYBEZIERTO16` | 89 | ⬜ | 16-bit variant |
| `POLYDRAW` | 56 | ⬜ | Mixed line/bezier/move |
| `POLYDRAW16` | 92 | ⬜ | 16-bit variant |

### Path Bracket (BEGINPATH / ENDPATH)

> **Priority.** Many real-world EMF files wrap drawing commands in a path bracket.
> Without this, complex shapes with `STROKEPATH` / `FILLPATH` will not render.

| Record | Type ID | Status | Notes |
|--------|---------|--------|-------|
| `BEGINPATH` | 59 | ⬜ | Set `inPath = true`, open `<path d="` |
| `ENDPATH` | 60 | ⬜ | Set `inPath = false` |
| `CLOSEFIGURE` | 61 | ⬜ | Append `Z` to current path |
| `STROKEPATH` | 64 | ⬜ | Close path + stroke |
| `FILLPATH` | 62 | ⬜ | Close path + fill |
| `STROKEANDFILLPATH` | 63 | ⬜ | Close path + stroke + fill |
| `FLATTENPATH` | 65 | ⬜ | Can ignore |
| `WIDENPATH` | 66 | ⬜ | Can ignore |
| `SELECTCLIPPATH` | 67 | ⬜ | Clip from current path |
| `ABORTPATH` | 68 | ⬜ | Discard accumulated path |

### Object Creation

| Record | Type ID | Status | Notes |
|--------|---------|--------|-------|
| `CREATEPEN` | 38 | ✅ | |
| `EXTCREATEPEN` | 58 | ⬜ | Extended pen — same as CREATEPEN but reads `EXTLOGPEN` |
| `CREATEBRUSHINDIRECT` | 39 | ⬜ | Fill color/style |
| `CREATEDIBPATTERNBRUSHPT` | 94 | ⬜ | Bitmap brush — complex |
| `EXTCREATEFONTINDIRECTW` | 82 | ⬜ | Font — needed for text |
| `CREATEPALETTE` | 49 | ⬜ | Can ignore |
| `SELECTOBJECT` | 37 | ✅ | |
| `DELETEOBJECT` | 40 | ✅ | |

### State Records

| Record | Type ID | Status | Notes |
|--------|---------|--------|-------|
| `SAVEDC` | 33 | ✅ | |
| `RESTOREDC` | 34 | ✅ | |
| `SETMAPMODE` | 17 | ✅ | |
| `SETBKMODE` | 18 | ✅ | |
| `SETROP2` | 20 | ✅ | Ignored |
| `SETBKCOLOR` | 25 | ✅ | |
| `SETWINDOWEXTEX` | 9 | ✅ | |
| `SETVIEWPORTEXTEX` | 11 | ✅ | |
| `SETWINDOWORGEX` | 10 | ⬜ | Window origin offset |
| `SETVIEWPORTORGEX` | 12 | ⬜ | Viewport origin offset |
| `SETPOLYFILLMODE` | 19 | ⬜ | `fill-rule: evenodd/nonzero` |
| `SETMITERLIMIT` | 58 | ⬜ | `stroke-miterlimit` |
| `SETARCDIRECTION` | 57 | ⬜ | Arc sweep direction |
| `SETTEXTCOLOR` | 24 | ⬜ | Text fill color |
| `SETTEXTALIGN` | 22 | ⬜ | Text alignment |
| `SETSTRETCHBLTMODE` | 21 | ⬜ | Bitmap stretch — can ignore |
| `SETWORLDTRANSFORM` | 35 | ⬜ | **Priority.** 2D affine matrix — very common |
| `MODIFYWORLDTRANSFORM` | 36 | ⬜ | **Priority.** Modify current transform |

### Clipping

| Record | Type ID | Status | Notes |
|--------|---------|--------|-------|
| `INTERSECTCLIPRECT` | 30 | ✅ | |
| `EXCLUDECLIPRECT` | 29 | ⬜ | |
| `OFFSETCLIPRGN` | 26 | ⬜ | |
| `EXTSELECTCLIPRGN` | 75 | ⬜ | Complex region clipping |

### Text

| Record | Type ID | Status | Notes |
|--------|---------|--------|-------|
| `EXTTEXTOUTW` | 84 | ✅ | Skipped (nChars=0 in current test file) |
| `EXTTEXTOUTA` | 83 | ⬜ | ASCII variant |
| `SMALLTEXTOUT` | 108 | ⬜ | |

> Text rendering requires font lookup. In C# use `System.Drawing` or `SkiaSharp` to measure
> and place glyphs. The C reference uses fontconfig + FreeType.

### Bitmaps & Images

| Record | Type ID | Status | Notes |
|--------|---------|--------|-------|
| `BITBLT` | 76 | ⬜ | Blit with ROP |
| `STRETCHBLT` | 77 | ⬜ | Scaled blit |
| `STRETCHDIBITS` | 81 | ⬜ | DIB blit — most common |
| `ALPHABLEND` | 114 | ⬜ | Alpha-blended blit |
| `TRANSPARENTBLT` | 115 | ⬜ | Transparent blit |
| `SETDIBITSTODEVICE` | 80 | ⬜ | Direct DIB |
| `PLGBLT` | 79 | ⬜ | Parallelogram blit |

> Bitmap records require parsing DIB (Device-Independent Bitmap) headers and converting
> pixel data to Base64-encoded PNG for embedding in SVG as `<image>`.

### Control

| Record | Type ID | Status | Notes |
|--------|---------|--------|-------|
| `HEADER` | 1 | ✅ | |
| `EOF` | 14 | ✅ | |

---

## Development Tips

- **Reference implementation:** For any record, read the corresponding handler in
  `src/lib/emf2svg_rec_*.c` in the libemf2svg source to understand the expected SVG output.
- **Binary layout:** Every struct is documented in `vendor/libuemf/include/uemf.h`.
- **Test file:** Use `dotnet run -- --list-records -i yourfile.emf` to see which record
  types a file uses before implementing them.
- **Quirks:** See [`docs/NOTES.md`](docs/NOTES.md) for known C-compatibility quirks that
  are intentionally preserved.
