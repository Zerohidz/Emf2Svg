namespace Emf2Svg.Tests;

/// <summary>
/// MS-EMF: the DC state saved by SAVEDC and restored by RESTOREDC includes the
/// mapping mode and the window/viewport origins and extents. Anything a record
/// changes between SAVEDC and its matching RESTOREDC must not survive the restore.
/// </summary>
public class SaveRestoreDcTests
{
    private const double Tol = 1e-4;

    // 6:5 window-to-viewport ratio on x, 6:6 on y — the shape Metalix emits around
    // its dimension arrows: x is squeezed to 5/6, y is untouched.
    private static EmfBuilder AnisotropicBlock(EmfBuilder b) =>
        b.SetMapMode(EmfBuilder.MM_ANISOTROPIC).SetWindowExtEx(6, 6).SetViewportExtEx(5, 6);

    [Fact]
    public void RestoreDc_RevertsAnisotropicExtents()
    {
        var svg = AnisotropicBlock(new EmfBuilder()
                .Header(0, 0, 100, 100)
                .Line(0, 0, 100, 0)      // reference line, drawn before the block
                .SaveDC())
            .Line(0, 10, 100, 10)        // inside the block: squeezed to 5/6
            .RestoreDC(-1)
            .Line(0, 20, 100, 20)        // after the block: must match the reference line
            .Eof()
            .ToSvg();

        var lines = SvgLines.Parse(svg);
        Assert.Equal(3, lines.Count);
        Assert.Equal(100.0, lines[0].Width, Tol);
        Assert.Equal(100.0 * 5 / 6, lines[1].Width, Tol);
        Assert.Equal(lines[0].Width, lines[2].Width, Tol);
    }

    [Fact]
    public void RectangleDrawnAfterScopedMappingKeepsFullWidth()
    {
        // The shape of the real-world defect: a full-width outline is drawn, then a
        // short anisotropic block scales x by 5/6 for a few dimension marks, then a
        // second outline of the same nominal width is drawn. Both outlines must come
        // out the same width — that is the invariant the bug broke.
        var b = new EmfBuilder().Header(0, 0, 813, 306);
        Rect(b, 40, 10, 774, 289);          // sheet outline, before the block
        b.SaveDC();
        AnisotropicBlock(b);
        b.Line(600, 100, 700, 100);         // dimension mark inside the block
        b.RestoreDC(-1);
        Rect(b, 40, 12, 774, 287);          // usable-area outline, after the block

        var lines = SvgLines.Parse(b.Eof().ToSvg());
        Assert.Equal(9, lines.Count);

        double sheetWidth = lines[0].Width;              // top edge of the first rect
        double markWidth = lines[4].Width;               // the scoped dimension mark
        double usableWidth = lines[5].Width;             // top edge of the second rect

        Assert.Equal(734.0, sheetWidth, Tol);
        Assert.Equal(100.0 * 5 / 6, markWidth, Tol);
        Assert.Equal(sheetWidth, usableWidth, Tol);
    }

    private static void Rect(EmfBuilder b, int left, int top, int right, int bottom)
    {
        b.Line(left, top, right, top);
        b.Line(right, top, right, bottom);
        b.Line(right, bottom, left, bottom);
        b.Line(left, bottom, left, top);
    }

    [Fact]
    public void RestoreDc_RevertsMapMode()
    {
        // Extents are set up front while MM_TEXT ignores them; only the SETMAPMODE
        // inside the block activates them. Restoring must deactivate them again.
        var svg = new EmfBuilder()
            .Header(0, 0, 100, 100)
            .SetWindowExtEx(6, 6)
            .SetViewportExtEx(5, 6)
            .Line(0, 0, 100, 0)
            .SaveDC()
            .SetMapMode(EmfBuilder.MM_ANISOTROPIC)
            .Line(0, 10, 100, 10)
            .RestoreDC(-1)
            .Line(0, 20, 100, 20)
            .Eof()
            .ToSvg();

        var lines = SvgLines.Parse(svg);
        Assert.Equal(3, lines.Count);
        Assert.Equal(100.0, lines[0].Width, Tol);
        Assert.Equal(100.0 * 5 / 6, lines[1].Width, Tol);
        Assert.Equal(100.0, lines[2].Width, Tol);
    }

    [Fact]
    public void SetViewportOrgEx_ShiftsSubsequentPoints()
    {
        var svg = new EmfBuilder()
            .Header(0, 0, 100, 100)
            .SetMapMode(EmfBuilder.MM_ANISOTROPIC)
            .SetViewportOrgEx(30, 5)
            .Line(0, 0, 100, 0)
            .Eof()
            .ToSvg();

        var line = Assert.Single(SvgLines.Parse(svg));
        Assert.Equal(30.0, line.X1, Tol);
        Assert.Equal(5.0, line.Y1, Tol);
        Assert.Equal(130.0, line.X2, Tol);
    }

    [Fact]
    public void RestoreDc_RevertsViewportOrigin()
    {
        var svg = new EmfBuilder()
            .Header(0, 0, 100, 100)
            .SetMapMode(EmfBuilder.MM_ANISOTROPIC)
            .Line(0, 0, 100, 0)
            .SaveDC()
            .SetViewportOrgEx(30, 0)
            .Line(0, 10, 100, 10)
            .RestoreDC(-1)
            .Line(0, 20, 100, 20)
            .Eof()
            .ToSvg();

        var lines = SvgLines.Parse(svg);
        Assert.Equal(3, lines.Count);
        Assert.Equal(0.0, lines[0].X1, Tol);
        Assert.Equal(30.0, lines[1].X1, Tol);
        Assert.Equal(0.0, lines[2].X1, Tol);
        Assert.Equal(100.0, lines[2].X2, Tol);
    }

    [Fact]
    public void SetWindowOrgEx_ShiftsSubsequentPoints()
    {
        var svg = new EmfBuilder()
            .Header(0, 0, 100, 100)
            .SetMapMode(EmfBuilder.MM_ANISOTROPIC)
            .SetWindowOrgEx(20, 0)
            .Line(0, 0, 100, 0)
            .Eof()
            .ToSvg();

        var line = Assert.Single(SvgLines.Parse(svg));
        Assert.Equal(-20.0, line.X1, Tol);
        Assert.Equal(80.0, line.X2, Tol);
    }

    [Fact]
    public void RestoreDc_RevertsWindowOrigin()
    {
        var svg = new EmfBuilder()
            .Header(0, 0, 100, 100)
            .SetMapMode(EmfBuilder.MM_ANISOTROPIC)
            .Line(0, 0, 100, 0)
            .SaveDC()
            .SetWindowOrgEx(20, 0)
            .Line(0, 10, 100, 10)
            .RestoreDC(-1)
            .Line(0, 20, 100, 20)
            .Eof()
            .ToSvg();

        var lines = SvgLines.Parse(svg);
        Assert.Equal(3, lines.Count);
        Assert.Equal(0.0, lines[0].X1, Tol);
        Assert.Equal(-20.0, lines[1].X1, Tol);
        Assert.Equal(0.0, lines[2].X1, Tol);
    }

    [Fact]
    public void RestoreDc_RevertsExtentsSetFlagSoLaterMapModeChangeIsInert()
    {
        // WindowExSet/ViewPortExSet are part of the saved state too: if the flags
        // leak out of the block, a later SETMAPMODE picks up extents that were
        // never set in the restored context.
        var svg = AnisotropicBlock(new EmfBuilder()
                .Header(0, 0, 100, 100)
                .SaveDC())
            .RestoreDC(-1)
            .SetMapMode(EmfBuilder.MM_ANISOTROPIC)   // no extents set in this context
            .Line(0, 0, 100, 0)
            .Eof()
            .ToSvg();

        var line = Assert.Single(SvgLines.Parse(svg));
        Assert.Equal(100.0, line.Width, Tol);
    }

    [Fact]
    public void NestedRestoreDc_UnwindsOneLevelPerCall()
    {
        var svg = new EmfBuilder()
            .Header(0, 0, 100, 100)
            .SaveDC()                                       // level 1: MM_TEXT
            .SetMapMode(EmfBuilder.MM_ANISOTROPIC).SetWindowExtEx(6, 6).SetViewportExtEx(3, 6)
            .SaveDC()                                       // level 2: x scale 1/2
            .SetViewportExtEx(5, 6)                         // x scale 5/6
            .Line(0, 0, 100, 0)
            .RestoreDC(-1)                                  // back to level 2 state: 1/2
            .Line(0, 10, 100, 10)
            .RestoreDC(-1)                                  // back to level 1 state: MM_TEXT
            .Line(0, 20, 100, 20)
            .Eof()
            .ToSvg();

        var lines = SvgLines.Parse(svg);
        Assert.Equal(3, lines.Count);
        Assert.Equal(100.0 * 5 / 6, lines[0].Width, Tol);
        Assert.Equal(50.0, lines[1].Width, Tol);
        Assert.Equal(100.0, lines[2].Width, Tol);
    }

    [Fact]
    public void RestoreDc_WithRelativeMinusTwo_DiscardsTheSkippedLevel()
    {
        var svg = new EmfBuilder()
            .Header(0, 0, 100, 100)
            .SaveDC()                                       // level 1: MM_TEXT
            .SetMapMode(EmfBuilder.MM_ANISOTROPIC).SetWindowExtEx(6, 6).SetViewportExtEx(3, 6)
            .SaveDC()                                       // level 2: x scale 1/2
            .SetViewportExtEx(5, 6)
            .SaveDC()                                       // level 3: x scale 5/6
            .SetViewportExtEx(1, 6)
            .RestoreDC(-2)                                  // two levels back: level 2 state, 1/2
            .Line(0, 0, 100, 0)
            .RestoreDC(-1)                                  // level 1 state: MM_TEXT
            .Line(0, 10, 100, 10)
            .Eof()
            .ToSvg();

        var lines = SvgLines.Parse(svg);
        Assert.Equal(2, lines.Count);
        Assert.Equal(50.0, lines[0].Width, Tol);
        Assert.Equal(100.0, lines[1].Width, Tol);
    }

    [Fact]
    public void RestoreDc_WithPositiveRelative_RestoresThatAbsoluteSavedState()
    {
        var svg = new EmfBuilder()
            .Header(0, 0, 100, 100)
            .SaveDC()                                       // saved state 1: MM_TEXT
            .SetMapMode(EmfBuilder.MM_ANISOTROPIC).SetWindowExtEx(6, 6).SetViewportExtEx(3, 6)
            .SaveDC()                                       // saved state 2
            .SetViewportExtEx(5, 6)
            .RestoreDC(1)                                   // absolute: back to saved state 1
            .Line(0, 0, 100, 0)
            .Eof()
            .ToSvg();

        var line = Assert.Single(SvgLines.Parse(svg));
        Assert.Equal(100.0, line.Width, Tol);
    }

    [Fact]
    public void RestoreDc_OnEmptyStack_IsIgnored()
    {
        var svg = new EmfBuilder()
            .Header(0, 0, 100, 100)
            .SetMapMode(EmfBuilder.MM_ANISOTROPIC).SetWindowExtEx(6, 6).SetViewportExtEx(5, 6)
            .RestoreDC(-1)
            .Line(0, 0, 100, 0)
            .Eof()
            .ToSvg();

        var line = Assert.Single(SvgLines.Parse(svg));
        Assert.Equal(100.0 * 5 / 6, line.Width, Tol);
    }

    [Fact]
    public void RestoreDc_OutOfRangeRelative_IsIgnored()
    {
        var svg = new EmfBuilder()
            .Header(0, 0, 100, 100)
            .SaveDC()
            .SetMapMode(EmfBuilder.MM_ANISOTROPIC).SetWindowExtEx(6, 6).SetViewportExtEx(5, 6)
            .RestoreDC(-5)
            .Line(0, 0, 100, 0)
            .Eof()
            .ToSvg();

        var line = Assert.Single(SvgLines.Parse(svg));
        Assert.Equal(100.0 * 5 / 6, line.Width, Tol);
    }
}
