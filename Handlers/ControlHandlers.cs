namespace Emf2Svg.Handlers;

public static class ControlHandlers
{
    // EMF HEADER record (iType=1)
    // Payload layout (after iType+nSize):
    //   rclBounds RectL (16), rclFrame RectL (16), dSignature uint32 (4),
    //   nVersion uint32 (4), nBytes uint32 (4), nRecords uint32 (4),
    //   nHandles uint16 (2), sReserved uint16 (2), nDescription uint32 (4),
    //   offDescription uint32 (4), nPalEntries uint32 (4),
    //   szlDevice SizeL (8), szlMillimeters SizeL (8)
    public static void HandleHeader(BinaryReader payload, DrawingState state, SvgWriter svg)
    {
        var rclBounds = payload.ReadRectL();
        var rclFrame = payload.ReadRectL();
        payload.ReadUInt32(); // dSignature
        payload.ReadUInt32(); // nVersion
        payload.ReadUInt32(); // nBytes
        payload.ReadUInt32(); // nRecords
        int nHandles = payload.ReadUInt16();
        payload.ReadUInt16(); // sReserved
        payload.ReadUInt32(); // nDescription
        payload.ReadUInt32(); // offDescription
        payload.ReadUInt32(); // nPalEntries
        var szlDevice = payload.ReadSizeL();
        var szlMillimeters = payload.ReadSizeL();

        state.Bounds = rclBounds;
        state.RefX = rclBounds.Left;
        state.RefY = rclBounds.Top;

        double w = Math.Abs(rclBounds.Right - rclBounds.Left);
        double h = Math.Abs(rclBounds.Bottom - rclBounds.Top);
        state.ImgWidth = w;
        state.ImgHeight = h;
        state.GlobalScaling = w / w; // = 1.0 (imgWidth / |bounds width|)

        state.PxPerMm = (double)szlDevice.CX / (double)szlMillimeters.CX;
        state.ObjectTable = new GdiObject[nHandles + 1];

        svg.WriteHeader(state);
    }

    // EMF EOF record (iType=14)
    public static void HandleEof(DrawingState state, SvgWriter svg)
    {
        svg.WriteFooter();
    }
}
