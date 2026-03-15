namespace Emf2Svg;

public struct PointL { public int X, Y; }
public struct SizeL { public int CX, CY; }
public struct RectL { public int Left, Top, Right, Bottom; }
public struct ColorRef { public byte R, G, B; }

public struct LogPen
{
    public uint Style;
    public PointL Width;
    public ColorRef Color;
}

public static class BinaryReaderExt
{
    public static PointL ReadPointL(this BinaryReader r) =>
        new PointL { X = r.ReadInt32(), Y = r.ReadInt32() };

    public static SizeL ReadSizeL(this BinaryReader r) =>
        new SizeL { CX = r.ReadInt32(), CY = r.ReadInt32() };

    public static RectL ReadRectL(this BinaryReader r) =>
        new RectL { Left = r.ReadInt32(), Top = r.ReadInt32(), Right = r.ReadInt32(), Bottom = r.ReadInt32() };

    public static ColorRef ReadColorRef(this BinaryReader r)
    {
        var c = new ColorRef { R = r.ReadByte(), G = r.ReadByte(), B = r.ReadByte() };
        r.ReadByte(); // reserved
        return c;
    }

    public static LogPen ReadLogPen(this BinaryReader r) =>
        new LogPen { Style = r.ReadUInt32(), Width = r.ReadPointL(), Color = r.ReadColorRef() };
}
