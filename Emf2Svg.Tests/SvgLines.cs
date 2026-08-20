using System.Globalization;
using System.Text.RegularExpressions;

namespace Emf2Svg.Tests;

/// <summary>A straight line path emitted for one LINETO record.</summary>
public record SvgLine(double X1, double Y1, double X2, double Y2)
{
    public double Width => X2 - X1;
    public override string ToString() =>
        $"({X1.ToString("0.####", CultureInfo.InvariantCulture)},{Y1.ToString("0.####", CultureInfo.InvariantCulture)})"
        + $"->({X2.ToString("0.####", CultureInfo.InvariantCulture)},{Y2.ToString("0.####", CultureInfo.InvariantCulture)})";
}

public static class SvgLines
{
    private static readonly Regex LinePath = new(
        @"d=""M\s*(-?[\d.]+),(-?[\d.]+)\s+L\s*(-?[\d.]+),(-?[\d.]+)\s*""",
        RegexOptions.Compiled);

    /// <summary>Extracts every "M x,y L x,y" path from an SVG document, in document order.</summary>
    public static List<SvgLine> Parse(string svg)
    {
        var result = new List<SvgLine>();
        foreach (Match m in LinePath.Matches(svg))
        {
            result.Add(new SvgLine(
                double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture),
                double.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture),
                double.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture),
                double.Parse(m.Groups[4].Value, CultureInfo.InvariantCulture)));
        }
        return result;
    }
}
