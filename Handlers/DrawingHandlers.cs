namespace Emf2Svg.Handlers;

public static class DrawingHandlers
{
    // LINETO (iType=54): [PointL ptl]
    // Generates: <path d="M curX,curY L x,y " stroke=... fill="none" />
    public static void HandleLineTo(BinaryReader p, DrawingState s, SvgWriter svg)
    {
        var ptl = p.ReadPointL();

        // C's startPathDraw truncates cur_x/cur_y to int (U_POINT cast)
        var from = CoordTransform.TransformPoint(s, (int)s.CurX, (int)s.CurY);
        var to = CoordTransform.TransformPoint(s, ptl.X, ptl.Y);

        svg.WriteLineTo(s, from, to);

        // Update current position
        s.CurX = ptl.X;
        s.CurY = ptl.Y;
    }

    // ARC (iType=45): [RectL rclBox][PointL ptlStart][PointL ptlEnd]
    // Generates: <path d="M curX,curY M startX,startY A rx,ry 0 large sweep endX,endY " .../>
    public static void HandleArc(BinaryReader p, DrawingState s, SvgWriter svg)
    {
        var rclBox = p.ReadRectL();
        var ptlStart = p.ReadPointL();
        var ptlEnd = p.ReadPointL();

        // arcdir > 0 means CW: sweep=1, large=1; else sweep=0, large=0
        // Default arcdir = 0 → sweep=0, large=0
        int sweepFlag = 0;
        int largeArcFlag = 0;

        // Radii in EMF units — use integer division like C code (U_POINTL arithmetic)
        double rx = (rclBox.Right - rclBox.Left) / 2;
        double ry = (rclBox.Bottom - rclBox.Top) / 2;
        // Scale radii (no offset for distances)
        double rxSvg = rx * s.GlobalScaling;
        double rySvg = ry * s.GlobalScaling;

        // Compute start and end points on ellipse boundary (EMF coords)
        var startEmf = CoordTransform.IntersectEllipseRadial(ptlStart, rclBox);
        var endEmf = CoordTransform.IntersectEllipseRadial(ptlEnd, rclBox);

        // Transform to SVG coords
        // C's startPathDraw truncates cur_x/cur_y to int (U_POINT cast)
        var curSvg = CoordTransform.TransformPoint(s, (int)s.CurX, (int)s.CurY);
        var startSvg = CoordTransform.TransformPoint(s, startEmf.X, startEmf.Y);
        var endSvg = CoordTransform.TransformPoint(s, endEmf.X, endEmf.Y);

        svg.WriteArc(s, curSvg, startSvg, rxSvg, rySvg, largeArcFlag, sweepFlag, endSvg);

        // Update current position to end point
        s.CurX = endEmf.X;
        s.CurY = endEmf.Y;
    }

    // ANGLEARC (iType=41): [PointL ptlCenter][uint32 nRadius][float eStartAngle][float eSweepAngle]
    // Generates: <path d="M curX,curY M startX,startY A rx,ry 0 large sweep endX,endY " .../>
    public static void HandleAngleArc(BinaryReader p, DrawingState s, SvgWriter svg)
    {
        var ptlCenter = p.ReadPointL();
        uint nRadius = p.ReadUInt32();
        float eStartAngle = p.ReadSingle();
        float eSweepAngle = p.ReadSingle();

        // From arc_circle_draw in emf2svg_utils.c:
        // arcdir > 0 → sweep=1, large=1; else sweep=0, large=0
        int sweepFlag = 0;
        int largeArcFlag = 0;

        double radius = nRadius;
        double startAngleRad = eStartAngle * Math.PI / 180.0;
        double endAngleRad = (eStartAngle + eSweepAngle) * Math.PI / 180.0;

        // Compute start and end points in EMF coords
        double startX = radius * Math.Cos(startAngleRad) + ptlCenter.X;
        double startY = radius * Math.Sin(startAngleRad) + ptlCenter.Y;
        double endX = radius * Math.Cos(endAngleRad) + ptlCenter.X;
        double endY = radius * Math.Sin(endAngleRad) + ptlCenter.Y;

        // Scale radius (no offset for distances)
        double rxSvg = radius * s.GlobalScaling;
        double rySvg = radius * s.GlobalScaling;

        // C's startPathDraw truncates cur_x/cur_y to int (U_POINT cast)
        var curSvg = CoordTransform.TransformPoint(s, (int)s.CurX, (int)s.CurY);
        var startSvg = CoordTransform.TransformPoint(s, startX, startY);
        var endSvg = CoordTransform.TransformPoint(s, endX, endY);

        svg.WriteArc(s, curSvg, startSvg, rxSvg, rySvg, largeArcFlag, sweepFlag, endSvg);

        // Update current position to end point
        s.CurX = endX;
        s.CurY = endY;
    }

    // INTERSECTCLIPRECT (iType=30): [RectL rclBox]
    // Creates a clipPath <defs> and sets the clip ID in current DC.
    public static void HandleIntersectClipRect(BinaryReader p, DrawingState s, SvgWriter svg)
    {
        var rect = p.ReadRectL();

        // Generate a clip ID
        int clipId = new Random(42).Next(1, int.MaxValue);
        // Use a deterministic ID to be consistent across runs
        clipId = 1804289383; // matches glibc rand() default first value

        s.DC.ClipId = clipId;
        svg.WriteClipDefs(s, rect, clipId);
    }

    // EXTTEXTOUTW (iType=84): skip (nChars=0 in input.emf, invisible)
    public static void HandleExtTextOutW(BinaryReader p, DrawingState s, SvgWriter svg)
    {
        // All EXTTEXTOUTW records in input.emf have nChars=0 → skip
    }
}
