using System.Xml.Linq;

namespace WgcColorCalibrator.Core.Tests.Resources;

public class ResourceKeyConsistencyTests
{
    [Fact]
    public void EnUsAndZhCn_ResourceKeys_AreConsistent()
    {
        string repoRoot = FindRepositoryRoot(AppContext.BaseDirectory)
            ?? throw new InvalidOperationException("Could not locate repository root from test output directory.");

        string enUsPath = Path.GetFullPath(Path.Combine(repoRoot, "src", "WgcColorCalibrator.App", "Strings", "en-US", "Resources.resw"));
        string zhCnPath = Path.GetFullPath(Path.Combine(repoRoot, "src", "WgcColorCalibrator.App", "Strings", "zh-CN", "Resources.resw"));

        HashSet<string> enUsKeys = LoadKeys(enUsPath);
        HashSet<string> zhCnKeys = LoadKeys(zhCnPath);

        Assert.Equal(enUsKeys, zhCnKeys);
    }

    private static string? FindRepositoryRoot(string startDirectory)
    {
        DirectoryInfo? directory = new(startDirectory);
        while (directory is not null)
        {
            if (directory.EnumerateDirectories(".git").Any())
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return null;
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
