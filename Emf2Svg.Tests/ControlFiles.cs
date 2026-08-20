namespace Emf2Svg.Tests;

/// <summary>
/// EMFs that do NOT use scoped mapping. Their SVG must be byte-identical before and
/// after the SAVEDC/RESTOREDC mapping-state fix — that is the regression contract.
/// </summary>
public static class ControlFiles
{
    public static IEnumerable<(string Name, string Svg)> All()
    {
        foreach (var (name, builder) in Builders())
            yield return (name, builder.ToSvg());
    }

    public static IEnumerable<(string Name, EmfBuilder Builder)> Builders()
    {
        // Plain MM_TEXT drawing: no mapping records at all.
        yield return ("plain-mm-text", new EmfBuilder()
            .Header(0, 0, 200, 150)
            .Line(10, 10, 190, 10)
            .Line(190, 10, 190, 140)
            .Line(190, 140, 10, 140)
            .Line(10, 140, 10, 10)
            .Eof());

        // Negative bounds origin, exercising the header translate.
        yield return ("shifted-bounds", new EmfBuilder()
            .Header(-20, -18, 813, 288)
            .Line(0, 0, 800, 0)
            .Line(0, 0, 0, 250)
            .Eof());

        // SAVEDC/RESTOREDC used without touching mapping state.
        yield return ("savedc-no-mapping", new EmfBuilder()
            .Header(0, 0, 100, 100)
            .Line(0, 0, 100, 0)
            .SaveDC()
            .Line(0, 10, 100, 10)
            .RestoreDC(-1)
            .Line(0, 20, 100, 20)
            .Eof());

        // Document-wide anisotropic mapping set once, never scoped — the common
        // legitimate use of SETMAPMODE, which must keep applying to everything after it.
        yield return ("global-anisotropic", new EmfBuilder()
            .Header(0, 0, 100, 100)
            .SetMapMode(EmfBuilder.MM_ANISOTROPIC)
            .SetWindowExtEx(6, 6)
            .SetViewportExtEx(5, 6)
            .Line(0, 0, 100, 0)
            .Line(0, 10, 100, 10)
            .Eof());

        // Isotropic mapping, likewise unscoped.
        yield return ("global-isotropic", new EmfBuilder()
            .Header(0, 0, 100, 100)
            .SetMapMode(EmfBuilder.MM_ISOTROPIC)
            .SetWindowExtEx(4, 4)
            .SetViewportExtEx(3, 4)
            .Line(0, 0, 100, 0)
            .Eof());
    }
}
