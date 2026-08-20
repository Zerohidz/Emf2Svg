using System.Text;

namespace Emf2Svg.Tests;

/// <summary>
/// Builds tiny, hand-written EMF byte streams for tests. Only the records the
/// converter actually dispatches are emitted; every record carries the exact
/// nSize the EMF spec prescribes so the reader's payload framing is exercised too.
/// </summary>
public class EmfBuilder
{
    private readonly List<byte[]> _records = new();

    private const uint EMR_HEADER = 1;
    private const uint EMR_SETWINDOWEXTEX = 9;
    private const uint EMR_SETWINDOWORGEX = 10;
    private const uint EMR_SETVIEWPORTEXTEX = 11;
    private const uint EMR_SETVIEWPORTORGEX = 12;
    private const uint EMR_EOF = 14;
    private const uint EMR_SETMAPMODE = 17;
    private const uint EMR_MOVETOEX = 27;
    private const uint EMR_SAVEDC = 33;
    private const uint EMR_RESTOREDC = 34;
    private const uint EMR_LINETO = 54;

    public const int MM_TEXT = 1;
    public const int MM_ISOTROPIC = 7;
    public const int MM_ANISOTROPIC = 8;

    /// <summary>EMR_HEADER with the given device-space bounds.</summary>
    public EmfBuilder Header(int left = 0, int top = 0, int right = 100, int bottom = 100)
    {
        var body = new MemoryStream();
        var w = new BinaryWriter(body);
        // rclBounds
        w.Write(left); w.Write(top); w.Write(right); w.Write(bottom);
        // rclFrame (0.01 mm units — unused by the converter, kept plausible)
        w.Write(left * 100); w.Write(top * 100); w.Write(right * 100); w.Write(bottom * 100);
        w.Write(0x464D4520u);   // dSignature " EMF"
        w.Write(0x00010000u);   // nVersion
        w.Write(0u);            // nBytes (patched in Build)
        w.Write(0u);            // nRecords (patched in Build)
        w.Write((ushort)16);    // nHandles
        w.Write((ushort)0);     // sReserved
        w.Write(0u);            // nDescription
        w.Write(0u);            // offDescription
        w.Write(0u);            // nPalEntries
        w.Write(1000); w.Write(1000);   // szlDevice
        w.Write(250); w.Write(250);     // szlMillimeters
        return Record(EMR_HEADER, body.ToArray());
    }

    public EmfBuilder MoveToEx(int x, int y) => Record(EMR_MOVETOEX, Ints(x, y));

    public EmfBuilder LineTo(int x, int y) => Record(EMR_LINETO, Ints(x, y));

    public EmfBuilder SaveDC() => Record(EMR_SAVEDC, Array.Empty<byte>());

    public EmfBuilder RestoreDC(int iRelative) => Record(EMR_RESTOREDC, Ints(iRelative));

    public EmfBuilder SetMapMode(int mode) => Record(EMR_SETMAPMODE, Ints(mode));

    public EmfBuilder SetWindowExtEx(int cx, int cy) => Record(EMR_SETWINDOWEXTEX, Ints(cx, cy));

    public EmfBuilder SetViewportExtEx(int cx, int cy) => Record(EMR_SETVIEWPORTEXTEX, Ints(cx, cy));

    public EmfBuilder SetWindowOrgEx(int x, int y) => Record(EMR_SETWINDOWORGEX, Ints(x, y));

    public EmfBuilder SetViewportOrgEx(int x, int y) => Record(EMR_SETVIEWPORTORGEX, Ints(x, y));

    public EmfBuilder Eof()
    {
        var body = new MemoryStream();
        var w = new BinaryWriter(body);
        w.Write(0u);    // nPalEntries
        w.Write(16u);   // offPalEntries
        w.Write(20u);   // nSizeLast
        return Record(EMR_EOF, body.ToArray());
    }

    /// <summary>Convenience: MOVETOEX + LINETO, the shape every scaling assertion uses.</summary>
    public EmfBuilder Line(int x1, int y1, int x2, int y2) => MoveToEx(x1, y1).LineTo(x2, y2);

    public byte[] Build()
    {
        var all = new MemoryStream();
        foreach (var r in _records) all.Write(r, 0, r.Length);
        var bytes = all.ToArray();

        // Patch nBytes / nRecords into the header (offsets: 8 + 16 + 16 + 4 + 4)
        if (_records.Count > 0)
        {
            BitConverter.GetBytes((uint)bytes.Length).CopyTo(bytes, 48);
            BitConverter.GetBytes((uint)_records.Count).CopyTo(bytes, 52);
        }
        return bytes;
    }

    /// <summary>Runs the converter over the built EMF and returns the SVG text.</summary>
    public string ToSvg()
    {
        using var input = new MemoryStream(Build());
        return EmfProcessor.ConvertToString(input);
    }

    private EmfBuilder Record(uint iType, byte[] body)
    {
        var rec = new MemoryStream();
        var w = new BinaryWriter(rec);
        w.Write(iType);
        w.Write((uint)(8 + body.Length));
        w.Write(body);
        _records.Add(rec.ToArray());
        return this;
    }

    private static byte[] Ints(params int[] values)
    {
        var ms = new MemoryStream();
        var w = new BinaryWriter(ms);
        foreach (var v in values) w.Write(v);
        return ms.ToArray();
    }
}
