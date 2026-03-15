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
