namespace Emf2Svg;

public class SvgWriter
{
    private readonly TextWriter _out;

    // Pen style constants (from uemf.h)
    private const int PS_NULL = 0x00000005;
    private const int PS_DASH = 0x00000001;
    private const int PS_DOT = 0x00000002;
    private const int PS_DASHDOT = 0x00000003;
    private const int PS_DASHDOTDOT = 0x00000004;
    private const int PS_GEOMETRIC = 0x00010000;
    private const int PS_ENDCAP_SQUARE = 0x00000100;
    private const int PS_ENDCAP_FLAT = 0x00000200;
    private const int PS_JOIN_BEVEL = 0x00001000;
    private const int PS_JOIN_MITER = 0x00002000;

    public SvgWriter(TextWriter writer) => _out = writer;

    public void WriteHeader(DrawingState s)
    {
        _out.WriteLine("<?xml version=\"1.0\"  encoding=\"UTF-8\" standalone=\"no\"?>");
        _out.Write("<svg version=\"1.1\" xmlns=\"http://www.w3.org/2000/svg\" ");
        _out.Write("xmlns:xlink=\"http://www.w3.org/1999/xlink\"");
        _out.WriteLine($" width=\"{s.ImgWidth:F4}\" height=\"{s.ImgHeight:F4}\">");
        _out.WriteLine($"<g transform=\"translate({-s.RefX * s.GlobalScaling:F4}, {-s.RefY * s.GlobalScaling:F4})\">");
    }

    public void WriteFooter()
    {
        _out.WriteLine("</g>");
        _out.WriteLine("</svg>");
    }

    // Write clip-path attribute if one is set
    private string ClipAttr(DeviceContext dc) =>
        dc.ClipId != 0 ? $" clip-path=\"url(#clip-{dc.ClipId})\" " : " ";

    // Write the clipPath <defs> block and set the clip ID
    public void WriteClipDefs(DrawingState s, RectL rect, int clipId)
    {
        var tf = CoordTransform.TransformPoint;
        var lt = tf(s, rect.Left, rect.Top);
        var rt = tf(s, rect.Right, rect.Top);
        var rb = tf(s, rect.Right, rect.Bottom);
        var lb = tf(s, rect.Left, rect.Bottom);

        _out.Write($"<defs><clipPath id=\"clip-{clipId}\">");
        _out.Write("<path d=\"");
        _out.Write($"M {lt.X:F4},{lt.Y:F4} ");
        _out.Write($"L {rt.X:F4},{rt.Y:F4} ");
        _out.Write($"L {rb.X:F4},{rb.Y:F4} ");
        _out.Write($"L {lb.X:F4},{lb.Y:F4} ");
        _out.Write($"L {lt.X:F4},{lt.Y:F4} ");
        _out.Write("Z Z\" />");
        _out.WriteLine("</clipPath></defs>");
    }

    // Writes stroke and fill attributes — equivalent to stroke_draw + "fill=\"none\""
    private void WriteStroke(DrawingState s)
    {
        var dc = s.DC;
        int mode = dc.StrokeMode;

        if ((mode & 0xFF) == PS_NULL)
        {
            // null pen: no stroke
            if (dc.FillMode != 5 /* U_BS_NULL */)
            {
                _out.Write($"stroke-width=\"1px\" stroke=\"#{dc.FillR:X2}{dc.FillG:X2}{dc.FillB:X2}\" ");
            }
            else
            {
                _out.Write("stroke=\"none\" stroke-width=\"0.0\" ");
            }
            _out.Write(" fill=\"none\" />\n");
            return;
        }

        // Color
        _out.Write($"stroke=\"#{dc.StrokeR:X2}{dc.StrokeG:X2}{dc.StrokeB:X2}\" ");

        // Width
        bool isGeometric = (mode & 0x000F0000) == PS_GEOMETRIC;
        double width = isGeometric ? dc.StrokeWidth : 1.0;
        double scaledWidth = CoordTransform.ScaleX(s, width);
        if ((scaledWidth / s.GlobalScaling) < 1.0)
            _out.Write("stroke-width=\"1px\" ");
        else
            _out.Write($"stroke-width=\"{scaledWidth:F4}\" ");

        // Dash pattern
        double unitStroke = dc.StrokeWidth * s.GlobalScaling;
        double dashLen = unitStroke * 5;
        double dotLen = unitStroke;
        switch (mode & 0xFF)
        {
            case PS_DASH:
                _out.Write($"stroke-dasharray=\"{dashLen:F4},{dashLen:F4}\" ");
                break;
            case PS_DOT:
                _out.Write($"stroke-dasharray=\"{dotLen:F4},{dotLen:F4}\" ");
                break;
            case PS_DASHDOT:
                _out.Write($"stroke-dasharray=\"{dashLen:F4},{dashLen:F4},{dotLen:F4},{dashLen:F4}\" ");
                break;
            case PS_DASHDOTDOT:
                _out.Write($"stroke-dasharray=\"{dashLen:F4},{dashLen:F4},{dotLen:F4},{dotLen:F4},{dotLen:F4},{dashLen:F4}\" ");
                break;
        }

        // Line cap
        switch (mode & 0x00000F00)
        {
            case 0x00000000: // PS_ENDCAP_ROUND
                _out.Write(" stroke-linecap=\"round\" ");
                break;
            case PS_ENDCAP_SQUARE:
                _out.Write(" stroke-linecap=\"square\" ");
                break;
            case PS_ENDCAP_FLAT:
                _out.Write(" stroke-linecap=\"butt\" ");
                break;
        }

        // Line join
        switch (mode & 0x0000F000)
        {
            case 0x00000000: // PS_JOIN_ROUND
                _out.Write(" stroke-linejoin=\"round\" ");
                break;
            case PS_JOIN_BEVEL:
                _out.Write(" stroke-linejoin=\"bevel\" ");
                break;
            case PS_JOIN_MITER:
                _out.Write(" stroke-linejoin=\"miter\" ");
                break;
        }

        _out.Write(" fill=\"none\" />\n");
    }

    // Write a LINETO path element:
    // <path clip-path="..." d="M curX,curY L x,y " stroke=... fill="none" />
    public void WriteLineTo(DrawingState s, PointD from, PointD to)
    {
        _out.Write($"<path {ClipAttr(s.DC)}d=\"M {from.X:F4},{from.Y:F4} L {to.X:F4},{to.Y:F4} \"");
        _out.Write(" ");
        WriteStroke(s);
    }

    // Write an ARC path element:
    // <path clip-path="..." d="M curX,curY M startX,startY A rx,ry 0 large sweep endX,endY " stroke=... fill="none" />
    public void WriteArc(DrawingState s, PointD curPos, PointD start, double rx, double ry,
                         int largeArc, int sweep, PointD end)
    {
        _out.Write($"<path {ClipAttr(s.DC)}d=\"M {curPos.X:F4},{curPos.Y:F4} ");
        _out.Write($"M {start.X:F4},{start.Y:F4} ");
        _out.Write($"A {rx:F4},{ry:F4} 0 {largeArc} {sweep} {end.X:F4},{end.Y:F4} \"");
        _out.Write(" ");
        WriteStroke(s);
    }
}
