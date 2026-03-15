namespace Emf2Svg.Handlers;

public static class StateHandlers
{
    // SETMAPMODE (iType=17): [uint32 iMode]
    public static void HandleSetMapMode(BinaryReader p, DrawingState s)
    {
        s.MapMode = (int)p.ReadUInt32();
    }

    // SETBKMODE (iType=18): [uint32 iMode]
    public static void HandleSetBkMode(BinaryReader p, DrawingState s)
    {
        s.DC.BkMode = (int)p.ReadUInt32();
    }

    // SETROP2 (iType=20): ignored
    public static void HandleSetRop2(BinaryReader p, DrawingState s)
    {
        // ignored
    }

    // SETBKCOLOR (iType=25): [ColorRef crColor]
    public static void HandleSetBkColor(BinaryReader p, DrawingState s)
    {
        var color = p.ReadColorRef();
        s.DC.BkR = color.R;
        s.DC.BkG = color.G;
        s.DC.BkB = color.B;
    }

    // MOVETOEX (iType=27): [PointL ptl]
    public static void HandleMoveToEx(BinaryReader p, DrawingState s)
    {
        var pt = p.ReadPointL();
        s.CurX = pt.X;
        s.CurY = pt.Y;
    }

    // SAVEDC (iType=33): no payload
    public static void HandleSaveDC(DrawingState s)
    {
        s.DCStack.Add(s.DC.Clone());
    }

    // RESTOREDC (iType=34): [int32 iRelative]
    // iRelative is negative: -1 = restore most recent, -2 = second most, etc.
    public static void HandleRestoreDC(BinaryReader p, DrawingState s)
    {
        int iRelative = p.ReadInt32();
        // Navigate to abs(iRelative)-th entry from end (1-based from end)
        int targetIdx = s.DCStack.Count + iRelative; // iRelative is negative
        if (targetIdx >= 0 && targetIdx < s.DCStack.Count)
        {
            s.DC = s.DCStack[targetIdx].Clone();
        }
    }

    // SETWINDOWEXTEX (iType=9): [SizeL szlExtent]
    public static void HandleSetWindowExtEx(BinaryReader p, DrawingState s)
    {
        var sz = p.ReadSizeL();
        s.WindowExX = sz.CX;
        s.WindowExY = sz.CY;
        s.WindowExSet = true;
    }

    // SETVIEWPORTEXTEX (iType=11): [SizeL szlExtent]
    public static void HandleSetViewportExtEx(BinaryReader p, DrawingState s)
    {
        var sz = p.ReadSizeL();
        s.ViewPortExX = sz.CX;
        s.ViewPortExY = sz.CY;
        s.ViewPortExSet = true;
    }
}
