namespace Emf2Svg.Handlers;

public static class StateHandlers
{
    // SETMAPMODE (iType=17): [uint32 iMode]
    public static void HandleSetMapMode(BinaryReader p, DrawingState s)
    {
        s.DC.MapMode = (int)p.ReadUInt32();
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
    // Pushes a copy of the whole DC — including the mapping state — onto the stack.
    public static void HandleSaveDC(DrawingState s)
    {
        s.DCStack.Add(s.DC.Clone());
    }

    // RESTOREDC (iType=34): [int32 iRelative]
    // Negative iRelative counts back from the current state: -1 restores the most
    // recently saved one, -2 the one before it, and so on. Positive iRelative names
    // a saved state absolutely: 1 is the first SAVEDC of the metafile.
    // Either way the restored state and everything saved after it leave the stack,
    // so the next RESTOREDC(-1) sees the correct entry.
    public static void HandleRestoreDC(BinaryReader p, DrawingState s)
    {
        int iRelative = p.ReadInt32();

        int targetIdx = iRelative < 0
            ? s.DCStack.Count + iRelative   // -1 => last entry
            : iRelative - 1;                // 1 => first entry

        // Out of range (including RESTOREDC on an empty stack): ignore silently,
        // matching GDI, which fails the call and leaves the DC untouched.
        if (targetIdx < 0 || targetIdx >= s.DCStack.Count)
            return;

        s.DC = s.DCStack[targetIdx];
        s.DCStack.RemoveRange(targetIdx, s.DCStack.Count - targetIdx);
    }

    // SETWINDOWEXTEX (iType=9): [SizeL szlExtent]
    public static void HandleSetWindowExtEx(BinaryReader p, DrawingState s)
    {
        var sz = p.ReadSizeL();
        s.DC.WindowExX = sz.CX;
        s.DC.WindowExY = sz.CY;
        s.DC.WindowExSet = true;
    }

    // SETWINDOWORGEX (iType=10): [PointL ptlOrigin]
    public static void HandleSetWindowOrgEx(BinaryReader p, DrawingState s)
    {
        var pt = p.ReadPointL();
        s.DC.WindowOrgX = pt.X;
        s.DC.WindowOrgY = pt.Y;
    }

    // SETVIEWPORTEXTEX (iType=11): [SizeL szlExtent]
    public static void HandleSetViewportExtEx(BinaryReader p, DrawingState s)
    {
        var sz = p.ReadSizeL();
        s.DC.ViewPortExX = sz.CX;
        s.DC.ViewPortExY = sz.CY;
        s.DC.ViewPortExSet = true;
    }

    // SETVIEWPORTORGEX (iType=12): [PointL ptlOrigin]
    public static void HandleSetViewportOrgEx(BinaryReader p, DrawingState s)
    {
        var pt = p.ReadPointL();
        s.DC.ViewPortOrgX = pt.X;
        s.DC.ViewPortOrgY = pt.Y;
    }
}
