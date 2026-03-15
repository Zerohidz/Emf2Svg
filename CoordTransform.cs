namespace Emf2Svg;

public struct PointD { public double X, Y; }

public static class CoordTransform
{
    // Equivalent to point_cal() in emf2svg_utils.c
    public static PointD TransformPoint(DrawingState s, double x, double y)
    {
        double scalingX, scalingY;
        double windowOrgX = 0, windowOrgY = 0;
        double viewPortOrgX = 0, viewPortOrgY = 0;

        switch (s.MapMode)
        {
            case 1: // U_MM_TEXT
                scalingX = 1.0;
                scalingY = 1.0;
                break;
            case 7: // U_MM_ISOTROPIC
                scalingX = (s.WindowExSet && s.ViewPortExSet) ? s.ViewPortExX / s.WindowExX : 1.0;
                scalingY = scalingX;
                windowOrgX = s.WindowOrgX; windowOrgY = s.WindowOrgY;
                viewPortOrgX = s.ViewPortOrgX; viewPortOrgY = s.ViewPortOrgY;
                break;
            case 8: // U_MM_ANISOTROPIC
                if (s.WindowExSet && s.ViewPortExSet)
                {
                    scalingX = s.ViewPortExX / s.WindowExX;
                    scalingY = s.ViewPortExY / s.WindowExY;
                }
                else
                {
                    scalingX = 1.0;
                    scalingY = 1.0;
                }
                windowOrgX = s.WindowOrgX; windowOrgY = s.WindowOrgY;
                viewPortOrgX = s.ViewPortOrgX; viewPortOrgY = s.ViewPortOrgY;
                break;
            default:
                scalingX = 1.0;
                scalingY = 1.0;
                break;
        }

        return new PointD
        {
            X = ((x - windowOrgX) * scalingX + viewPortOrgX) * s.GlobalScaling,
            Y = ((y - windowOrgY) * scalingY + viewPortOrgY) * s.GlobalScaling
        };
    }

    // Equivalent to scaleX() in emf2svg_utils.c for U_MM_TEXT
    public static double ScaleX(DrawingState s, double v)
    {
        double scalingX = s.MapMode switch
        {
            1 => 1.0, // U_MM_TEXT
            7 => (s.WindowExSet && s.ViewPortExSet) ? s.ViewPortExX / s.WindowExX : 1.0,
            8 => (s.WindowExSet && s.ViewPortExSet) ? s.ViewPortExX / s.WindowExX : 1.0,
            _ => 1.0
        };
        return v * scalingX * s.GlobalScaling;
    }

    // Compute the intersection of a radial from ellipse center through pt,
    // with the ellipse boundary. Equivalent to int_el_rad() in emf2svg_utils.c
    public static PointD IntersectEllipseRadial(PointL pt, RectL rect)
    {
        // Use integer division like C code (rect fields are int32, C does integer arithmetic)
        double centerX = (rect.Right + rect.Left) / 2;
        double centerY = (rect.Bottom + rect.Top) / 2;
        double radiusX = (rect.Right - rect.Left) / 2;
        double radiusY = (rect.Bottom - rect.Top) / 2;

        if (radiusX == 0 || radiusY == 0)
            return new PointD { X = centerX, Y = centerY };

        double ptNoX = pt.X - centerX;
        double ptNoY = pt.Y - centerY;

        if (ptNoX == 0)
            return new PointD { X = centerX, Y = Math.Sign(ptNoY) * radiusY + centerY };

        if (ptNoY == 0)
            return new PointD { X = Math.Sign(ptNoX) * radiusX + centerX, Y = centerY };

        double slope = ptNoY / ptNoX;
        double ix = Math.Sign(ptNoX) * Math.Sqrt(1.0 / (Math.Pow(1.0 / radiusX, 2) + Math.Pow(slope / radiusY, 2))) + centerX;
        double iy = Math.Sign(ptNoY) * Math.Sqrt(1.0 / (Math.Pow(1.0 / (slope * radiusX), 2) + Math.Pow(1.0 / radiusY, 2))) + centerY;

        return new PointD { X = ix, Y = iy };
    }
}
