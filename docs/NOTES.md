# Implementation Notes

## Intentional Quirks (C Compatibility)

These are behaviors inherited from the reference implementation (`libemf2svg`) that are
technically imprecise but intentionally replicated here to produce bit-identical SVG output.

---

### 1. Integer Division for Arc Radii

**Location:** `CoordTransform.IntersectEllipseRadial`, `DrawingHandlers.HandleArc`

In the C reference code, arc bounding box radii are computed using integer arithmetic:

```c
// src/lib/emf2svg_utils.c — arc_draw()
radii.x = (pEmr->rclBox.right - pEmr->rclBox.left) / 2;  // int / int = int
radii.y = (pEmr->rclBox.bottom - pEmr->rclBox.top) / 2;  // int / int = int
```

Because `rclBox` fields are `int32_t`, the division truncates. A bounding box with an odd
width (e.g. 5 pixels wide) produces a radius of `2`, not `2.5`.

The same applies inside `int_el_rad()` where the ellipse center and radii are computed:

```c
center.x = (rect.right + rect.left) / 2;  // integer division
radii.x  = (rect.right - rect.left) / 2;  // integer division
```

**Effect:** Sub-pixel inaccuracy (≤ 0.5 px) for arcs with odd-width bounding boxes.

**Why we keep it:** To match `libemf2svg` output exactly. If the upstream ever fixes this,
update both `CoordTransform.IntersectEllipseRadial` and `DrawingHandlers.HandleArc` to use
`/ 2.0` instead of `/ 2`.

---

### 2. Integer Truncation of Current Position in Path Start

**Location:** `DrawingHandlers.HandleLineTo`, `DrawingHandlers.HandleArc`, `DrawingHandlers.HandleAngleArc`

The C code's `startPathDraw()` assigns the current position into a `U_POINT` (int32) before
transforming it:

```c
// src/lib/emf2svg_utils.c — startPathDraw()
U_POINT pt;
pt.x = states->cur_x;  // double → int32 truncation toward zero
pt.y = states->cur_y;
point_draw(states, pt, out);
```

`cur_x` / `cur_y` are `double` internally, but get truncated to `int` here. This matters
when an `ARC` or `ANGLEARC` record updates `cur_x`/`cur_y` to a fractional value (e.g.
`96.2218`). The *next* path element's opening `M` command will use the truncated value
`96.0000` instead.

```
After ARC:  cur_x = 96.2218   (fractional, set by int_el_rad)
Next path:  M 96.0000,...      (truncated by startPathDraw cast)
```

**Effect:** The opening `M` of each path element may be up to 1 pixel off from the true
current pen position. This is visually imperceptible.

**Why we keep it:** To match `libemf2svg` output exactly. To fix it, remove the `(int)` cast
in the three `DrawingHandlers` methods and pass `s.CurX` / `s.CurY` directly.

---

## Deliberate Deviations from the Reference Output

Unlike the quirks above, these are places where matching `libemf2svg` byte-for-byte was
given up because the reference behaviour is wrong.

---

### 1. Mapping State Belongs to the Device Context

**Location:** `DrawingState.DeviceContext`, `Handlers/StateHandlers.cs`, `CoordTransform.cs`

Per MS-EMF, the state that `SAVEDC` saves and `RESTOREDC` restores includes the mapping
mode and the window/viewport origins and extents. Metafiles rely on this: a producer will
open a short block to draw a handful of marks under a different scale, then close it.

```
SAVEDC
SETMAPMODE(MM_ANISOTROPIC)
SETWINDOWEXTEX   (6, 6)
SETVIEWPORTEXTEX (5, 6)     → x scaled to 5/6 for the marks below
ARCTO × 7
RESTOREDC(-1)               → scale must be back to 1:1 here
```

These eleven fields therefore live on `DeviceContext`, not on `DrawingState`:

```
MapMode
WindowOrgX   WindowOrgY   WindowExX   WindowExY   WindowExSet
ViewPortOrgX ViewPortOrgY ViewPortExX ViewPortExY ViewPortExSet
```

`DeviceContext.Clone()` is a `MemberwiseClone()`, so `SAVEDC` picks them up automatically.
The `…ExSet` flags are part of the saved state too — if they leaked past a restore, a later
`SETMAPMODE` in the restored context would activate extents that were never set in it.

**Effect:** a scoped mapping block no longer bleeds into every record that follows it. Files
that set their mapping once and never scope it are unaffected — see the golden files in
`Emf2Svg.Tests/Golden/`.

---

### 2. RESTOREDC Unwinds the Stack

**Location:** `Handlers/StateHandlers.cs` — `HandleRestoreDC`

`RESTOREDC` used to index into the saved-state list and leave it intact, so the list only
ever grew. With two nested blocks the second `RESTOREDC(-1)` re-read the same entry the
first one did instead of the level above it.

`iRelative` is now handled the way GDI's `RestoreDC` documents it:

- **negative** — relative: `-1` is the most recent saved state, `-2` the one before it;
- **positive** — absolute: `1` is the first `SAVEDC` in the metafile;
- the restored entry and everything saved after it are discarded, so the next `RESTOREDC(-1)`
  lands on the right level;
- an index outside the stack — including `RESTOREDC` with nothing saved — is ignored and
  leaves the DC untouched, matching GDI, where the call simply fails.
