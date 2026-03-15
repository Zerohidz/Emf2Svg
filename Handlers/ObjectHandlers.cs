namespace Emf2Svg.Handlers;

public static class ObjectHandlers
{
    // Stock object index base (0x80000000 bit set means stock object)
    private const uint StockObjectBit = 0x80000000;

    // Stock object indices (from uemf.h)
    private const uint U_WHITE_BRUSH = 0x80000000;
    private const uint U_LTGRAY_BRUSH = 0x80000001;
    private const uint U_GRAY_BRUSH = 0x80000002;
    private const uint U_DKGRAY_BRUSH = 0x80000003;
    private const uint U_BLACK_BRUSH = 0x80000004;
    private const uint U_NULL_BRUSH = 0x80000005;
    private const uint U_WHITE_PEN = 0x80000006;
    private const uint U_BLACK_PEN = 0x80000007;
    private const uint U_NULL_PEN = 0x80000008;

    // PS_NULL pen style
    private const int PS_NULL = 0x00000005;

    // CREATEPEN (iType=38): [uint32 ihPen][LogPen lopn]
    public static void HandleCreatePen(BinaryReader p, DrawingState s)
    {
        uint index = p.ReadUInt32();
        var lopn = p.ReadLogPen();

        if (s.ObjectTable == null || index >= s.ObjectTable.Length) return;

        var obj = new GdiObject
        {
            StrokeSet = true,
            StrokeR = lopn.Color.R,
            StrokeG = lopn.Color.G,
            StrokeB = lopn.Color.B,
            StrokeMode = (int)lopn.Style,
            StrokeWidth = lopn.Width.X
        };
        s.ObjectTable[index] = obj;
    }

    // SELECTOBJECT (iType=37): [uint32 ihObject]
    public static void HandleSelectObject(BinaryReader p, DrawingState s)
    {
        uint index = p.ReadUInt32();

        if ((index & StockObjectBit) != 0)
        {
            // Stock object
            switch (index)
            {
                case U_WHITE_BRUSH:
                    s.DC.FillR = 0xFF; s.DC.FillG = 0xFF; s.DC.FillB = 0xFF;
                    s.DC.FillMode = 0; // BS_SOLID
                    break;
                case U_LTGRAY_BRUSH:
                    s.DC.FillR = 0xC0; s.DC.FillG = 0xC0; s.DC.FillB = 0xC0;
                    s.DC.FillMode = 0;
                    break;
                case U_GRAY_BRUSH:
                    s.DC.FillR = 0x80; s.DC.FillG = 0x80; s.DC.FillB = 0x80;
                    s.DC.FillMode = 0;
                    break;
                case U_DKGRAY_BRUSH:
                    s.DC.FillR = 0x40; s.DC.FillG = 0x40; s.DC.FillB = 0x40;
                    s.DC.FillMode = 0;
                    break;
                case U_BLACK_BRUSH:
                    s.DC.FillR = 0x00; s.DC.FillG = 0x00; s.DC.FillB = 0x00;
                    s.DC.FillMode = 0;
                    break;
                case U_NULL_BRUSH:
                    s.DC.FillMode = 5; // BS_NULL
                    break;
                case U_WHITE_PEN:
                    s.DC.StrokeR = 0xFF; s.DC.StrokeG = 0xFF; s.DC.StrokeB = 0xFF;
                    s.DC.StrokeMode = 0; // PS_SOLID
                    break;
                case U_BLACK_PEN:
                    s.DC.StrokeR = 0x00; s.DC.StrokeG = 0x00; s.DC.StrokeB = 0x00;
                    s.DC.StrokeMode = 0;
                    break;
                case U_NULL_PEN:
                    s.DC.StrokeMode = PS_NULL;
                    break;
                // font stock objects: ignored
            }
        }
        else
        {
            // User object
            if (s.ObjectTable == null || index >= s.ObjectTable.Length) return;
            var obj = s.ObjectTable[index];
            if (obj == null) return;

            if (obj.StrokeSet)
            {
                s.DC.StrokeR = obj.StrokeR;
                s.DC.StrokeG = obj.StrokeG;
                s.DC.StrokeB = obj.StrokeB;
                s.DC.StrokeMode = obj.StrokeMode;
                s.DC.StrokeWidth = obj.StrokeWidth;
            }
            else if (obj.FillSet)
            {
                s.DC.FillR = obj.FillR;
                s.DC.FillG = obj.FillG;
                s.DC.FillB = obj.FillB;
                s.DC.FillMode = obj.FillMode;
            }
        }
    }

    // DELETEOBJECT (iType=40): [uint32 ihObject]
    public static void HandleDeleteObject(BinaryReader p, DrawingState s)
    {
        uint index = p.ReadUInt32();
        if (s.ObjectTable != null && index < s.ObjectTable.Length)
            s.ObjectTable[index] = null!;
    }
}
