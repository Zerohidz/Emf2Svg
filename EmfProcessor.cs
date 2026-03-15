using Emf2Svg.Handlers;

namespace Emf2Svg;

// EMF record type constants
public static class EmfRecordType
{
    public const uint Header = 1;
    public const uint EOF = 14;
    public const uint SetWindowExtEx = 9;
    public const uint SetViewportExtEx = 11;
    public const uint SetMapMode = 17;
    public const uint SetBkMode = 18;
    public const uint SetRop2 = 20;
    public const uint SetBkColor = 25;
    public const uint MoveToEx = 27;
    public const uint IntersectClipRect = 30;
    public const uint SaveDC = 33;
    public const uint RestoreDC = 34;
    public const uint SelectObject = 37;
    public const uint CreatePen = 38;
    public const uint DeleteObject = 40;
    public const uint AngleArc = 41;
    public const uint Arc = 45;
    public const uint LineTo = 54;
    public const uint ExtTextOutW = 84;
}

public class EmfProcessor
{
    /// <summary>Converts an EMF stream to SVG, writing output to a Stream.</summary>
    public static void Process(Stream input, Stream output)
    {
        using var writer = new StreamWriter(output, System.Text.Encoding.UTF8, leaveOpen: true);
        Process(input, writer);
    }

    /// <summary>Converts an EMF stream to SVG, writing output to a TextWriter.</summary>
    public static void Process(Stream input, TextWriter output)
    {
        using var reader = new BinaryReader(input, System.Text.Encoding.UTF8, leaveOpen: true);
        var svg = new SvgWriter(output);
        var state = new DrawingState();
        _ProcessCore(reader, input, svg, state);
    }

    /// <summary>Converts an EMF file to SVG and returns the SVG content as a string.</summary>
    public static string ConvertToString(string inputPath)
    {
        using var fs = File.OpenRead(inputPath);
        return ConvertToString(fs);
    }

    /// <summary>Converts an EMF stream to SVG and returns the SVG content as a string.</summary>
    public static string ConvertToString(Stream input)
    {
        using var sw = new System.IO.StringWriter();
        Process(input, sw);
        return sw.ToString();
    }

    /// <summary>Converts an EMF file to an SVG file.</summary>
    public static void Process(string inputPath, string outputPath)
    {
        using var fs = File.OpenRead(inputPath);
        using var outFile = new StreamWriter(outputPath);
        Process(fs, outFile);
    }

    private static void _ProcessCore(BinaryReader reader, Stream fs, SvgWriter svg, DrawingState state)
    {
        while (fs.Position < fs.Length)
        {
            uint iType = reader.ReadUInt32();
            uint nSize = reader.ReadUInt32();

            if (nSize < 8) break; // malformed

            // Read the payload (nSize - 8 bytes for iType+nSize)
            int payloadSize = (int)(nSize - 8);
            byte[] payload = reader.ReadBytes(payloadSize);

            using var ms = new MemoryStream(payload);
            using var pr = new BinaryReader(ms);

            switch (iType)
            {
                case EmfRecordType.Header:
                    ControlHandlers.HandleHeader(pr, state, svg);
                    break;
                case EmfRecordType.EOF:
                    ControlHandlers.HandleEof(state, svg);
                    return;
                case EmfRecordType.SetMapMode:
                    StateHandlers.HandleSetMapMode(pr, state);
                    break;
                case EmfRecordType.SetBkMode:
                    StateHandlers.HandleSetBkMode(pr, state);
                    break;
                case EmfRecordType.SetRop2:
                    StateHandlers.HandleSetRop2(pr, state);
                    break;
                case EmfRecordType.SetBkColor:
                    StateHandlers.HandleSetBkColor(pr, state);
                    break;
                case EmfRecordType.MoveToEx:
                    StateHandlers.HandleMoveToEx(pr, state);
                    break;
                case EmfRecordType.SaveDC:
                    StateHandlers.HandleSaveDC(state);
                    break;
                case EmfRecordType.RestoreDC:
                    StateHandlers.HandleRestoreDC(pr, state);
                    break;
                case EmfRecordType.SetWindowExtEx:
                    StateHandlers.HandleSetWindowExtEx(pr, state);
                    break;
                case EmfRecordType.SetViewportExtEx:
                    StateHandlers.HandleSetViewportExtEx(pr, state);
                    break;
                case EmfRecordType.CreatePen:
                    ObjectHandlers.HandleCreatePen(pr, state);
                    break;
                case EmfRecordType.SelectObject:
                    ObjectHandlers.HandleSelectObject(pr, state);
                    break;
                case EmfRecordType.DeleteObject:
                    ObjectHandlers.HandleDeleteObject(pr, state);
                    break;
                case EmfRecordType.LineTo:
                    DrawingHandlers.HandleLineTo(pr, state, svg);
                    break;
                case EmfRecordType.Arc:
                    DrawingHandlers.HandleArc(pr, state, svg);
                    break;
                case EmfRecordType.AngleArc:
                    DrawingHandlers.HandleAngleArc(pr, state, svg);
                    break;
                case EmfRecordType.IntersectClipRect:
                    DrawingHandlers.HandleIntersectClipRect(pr, state, svg);
                    break;
                case EmfRecordType.ExtTextOutW:
                    DrawingHandlers.HandleExtTextOutW(pr, state, svg);
                    break;
                // All other record types: skip (payload already read)
            }
        }

        // If no EOF record, still close the SVG
        svg.WriteFooter();
    }

    /// <summary>Lists all EMF record types found in the file, with counts.</summary>
    public static void ListRecords(string inputPath)
    {
        using var fs = File.OpenRead(inputPath);
        using var reader = new BinaryReader(fs);

        var counts = new Dictionary<uint, int>();
        int total = 0;

        while (fs.Position < fs.Length)
        {
            uint iType = reader.ReadUInt32();
            uint nSize = reader.ReadUInt32();

            if (nSize < 8) break;

            counts.TryGetValue(iType, out int count);
            counts[iType] = count + 1;
            total++;

            if (iType == EmfRecordType.EOF) break;

            // Skip payload
            int skip = (int)(nSize - 8);
            if (skip > 0) reader.ReadBytes(skip);
        }

        Console.WriteLine($"Total records: {total}");
        foreach (var kvp in counts.OrderByDescending(x => x.Value))
        {
            string name = kvp.Key switch
            {
                EmfRecordType.Header => "HEADER",
                EmfRecordType.EOF => "EOF",
                EmfRecordType.SetWindowExtEx => "SETWINDOWEXTEX",
                EmfRecordType.SetViewportExtEx => "SETVIEWPORTEXTEX",
                EmfRecordType.SetMapMode => "SETMAPMODE",
                EmfRecordType.SetBkMode => "SETBKMODE",
                EmfRecordType.SetRop2 => "SETROP2",
                EmfRecordType.SetBkColor => "SETBKCOLOR",
                EmfRecordType.MoveToEx => "MOVETOEX",
                EmfRecordType.IntersectClipRect => "INTERSECTCLIPRECT",
                EmfRecordType.SaveDC => "SAVEDC",
                EmfRecordType.RestoreDC => "RESTOREDC",
                EmfRecordType.SelectObject => "SELECTOBJECT",
                EmfRecordType.CreatePen => "CREATEPEN",
                EmfRecordType.DeleteObject => "DELETEOBJECT",
                EmfRecordType.AngleArc => "ANGLEARC",
                EmfRecordType.Arc => "ARC",
                EmfRecordType.LineTo => "LINETO",
                EmfRecordType.ExtTextOutW => "EXTTEXTOUTW",
                _ => $"UNKNOWN({kvp.Key})"
            };
            Console.WriteLine($"  {name}: {kvp.Value}");
        }
    }
}
