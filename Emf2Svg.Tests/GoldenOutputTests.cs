namespace Emf2Svg.Tests;

/// <summary>
/// Characterisation tests: these goldens were captured from the converter *before*
/// the SAVEDC/RESTOREDC mapping-state fix. Files that do not scope their mapping
/// state must keep rendering byte-identically, so any diff here is a regression.
/// Regenerate only with a deliberate, explained output change.
/// </summary>
public class GoldenOutputTests
{
    public static TheoryData<string> Names()
    {
        var data = new TheoryData<string>();
        foreach (var (name, _) in ControlFiles.Builders()) data.Add(name);
        return data;
    }

    [Theory]
    [MemberData(nameof(Names))]
    public void ControlFileRendersUnchanged(string name)
    {
        var builder = ControlFiles.Builders().Single(b => b.Name == name).Builder;
        var expected = File.ReadAllText(Path.Combine(GoldenDir, name + ".svg"));

        Assert.Equal(Normalize(expected), Normalize(builder.ToSvg()));
    }

    private static string GoldenDir =>
        Path.Combine(AppContext.BaseDirectory, "Golden");

    private static string Normalize(string s) => s.Replace("\r\n", "\n");
}
