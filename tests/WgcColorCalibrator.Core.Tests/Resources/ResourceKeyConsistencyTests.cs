using System.Xml.Linq;

namespace WgcColorCalibrator.Core.Tests.Resources;

public class ResourceKeyConsistencyTests
{
    [Fact]
    public void EnUsAndZhCn_ResourceKeys_AreConsistent()
    {
        string enUsPath = Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "..",
            "src",
            "WgcColorCalibrator.App",
            "Strings",
            "en-US",
            "Resources.resw");
        string zhCnPath = Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "..",
            "src",
            "WgcColorCalibrator.App",
            "Strings",
            "zh-CN",
            "Resources.resw");

        // Tests run from the test project bin folder; resolve relative to repository root.
        string repoRoot = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..");
        enUsPath = Path.GetFullPath(Path.Combine(repoRoot, "src", "WgcColorCalibrator.App", "Strings", "en-US", "Resources.resw"));
        zhCnPath = Path.GetFullPath(Path.Combine(repoRoot, "src", "WgcColorCalibrator.App", "Strings", "zh-CN", "Resources.resw"));

        HashSet<string> enUsKeys = LoadKeys(enUsPath);
        HashSet<string> zhCnKeys = LoadKeys(zhCnPath);

        Assert.Equal(enUsKeys, zhCnKeys);
    }

    private static HashSet<string> LoadKeys(string path)
    {
        XDocument document = XDocument.Load(path);
        return document.Root!
            .Elements("data")
            .Select(element => element.Attribute("name")?.Value ?? string.Empty)
            .Where(name => !string.IsNullOrEmpty(name))
            .ToHashSet(StringComparer.Ordinal);
    }
}
