namespace Emf2Svg;

public class GdiObject
{
    public bool StrokeSet;
    public byte StrokeR, StrokeG, StrokeB;
    public int StrokeMode;   // lopnStyle (lower bits = line style, high bits = pen type)
    public double StrokeWidth;

    public bool FillSet;
    public byte FillR, FillG, FillB;
    public int FillMode;
}

public class DeviceContext
{
    // Stroke (pen) state
    public byte StrokeR = 0, StrokeG = 0, StrokeB = 0;
    public int StrokeMode = 0;      // 0 = PS_SOLID | PS_COSMETIC
    public double StrokeWidth = 1.0;

    // Fill (brush) state
    public byte FillR = 0xFF, FillG = 0xFF, FillB = 0xFF;
    public int FillMode = 0;        // 0 = BS_SOLID initially

    // Background
    public byte BkR, BkG, BkB;
    public int BkMode;

    // Clip
    public int ClipId;  // 0 = no clip

    public DeviceContext Clone() => (DeviceContext)MemberwiseClone();
}

public class DrawingState
{
    // From HEADER
    public double GlobalScaling = 1.0;
    public double RefX = 0, RefY = 0;
    public double PxPerMm = 1.0;
    public double ImgWidth, ImgHeight;
    public RectL Bounds;
    public GdiObject[]? ObjectTable;

    // Map mode and window/viewport extents
    public int MapMode = 1; // U_MM_TEXT
    public double WindowOrgX, WindowOrgY;
    public double WindowExX = 1, WindowExY = 1;
    public bool WindowExSet;
    public double ViewPortOrgX, ViewPortOrgY;
    public double ViewPortExX = 1, ViewPortExY = 1;
    public bool ViewPortExSet;

    // Current position (EMF coords)
    public double CurX, CurY;

    // Device context
    public DeviceContext DC = new DeviceContext();
    public List<DeviceContext> DCStack = new List<DeviceContext>();
}
