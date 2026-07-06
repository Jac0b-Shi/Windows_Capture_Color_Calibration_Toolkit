using System.Text.Json;

namespace WgcColorCalibrator.Core.Tests.Repository;

public sealed class RepositoryDocumentTests
{
    [Theory]
    [InlineData("README.md", "README.zh-CN.md")]
    [InlineData("README.zh-CN.md", "README.md")]
    [InlineData("docs/zh-CN/open-questions.md", "WGC-HDR-009")]
    [InlineData("docs/en-US/open-questions.md", "WGC-HDR-009")]
    public void RequiredDocuments_ExistAndContainExpectedText(string relativePath, string expectedText)
    {
        string fullPath = Path.Combine(GetRepositoryRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar));

        Assert.True(File.Exists(fullPath), $"Expected file to exist: {relativePath}");
        Assert.Contains(expectedText, File.ReadAllText(fullPath), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("schemas/chart-definition.schema.json")]
    [InlineData("schemas/measurement-profile.schema.json")]
    [InlineData("samples/charts/near-white.sample.json")]
    [InlineData("samples/charts/grayscale.sample.json")]
    public void JsonRepositoryFiles_AreValidJson(string relativePath)
    {
        string fullPath = Path.Combine(GetRepositoryRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar));

        using FileStream stream = File.OpenRead(fullPath);
        using JsonDocument document = JsonDocument.Parse(stream);

        Assert.Equal(JsonValueKind.Object, document.RootElement.ValueKind);
    }

    [Theory]
    [InlineData("WgcHdrColorCalibrator")]
    [InlineData("wgc-hdr-color-calibrator")]
    public void ActiveDocuments_DoNotContainLegacySolutionName(string legacyName)
    {
        string repositoryRoot = GetRepositoryRoot();

        // Only scan documentation, schemas, and automation files (not source or tests)
        string[] scanDirectories =
        [
            Path.Combine(repositoryRoot, "docs"),
            Path.Combine(repositoryRoot, "schemas"),
            Path.Combine(repositoryRoot, ".github"),
            repositoryRoot, // for README.md, README.zh-CN.md only
        ];

        // Files excluded from scan (historical docs that may keep the old name)
        var excludeRelativePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "docs/WGC_HDR_Color_Calibrator_Requirements_zh-CN.md",
            "docs/Milestone_0-2_Completion_Report.md",
        };

        var violations = new List<string>();

        foreach (string dir in scanDirectories)
        {
            if (!Directory.Exists(dir))
            {
                continue;
            }

            SearchOption searchOption = dir == repositoryRoot
                ? SearchOption.TopDirectoryOnly
                : SearchOption.AllDirectories;

            foreach (string file in Directory.GetFiles(dir, "*", searchOption))
            {
                // At repository root, only scan markdown files
                if (dir == repositoryRoot && !file.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // Skip binary files
                if (file.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) ||
                    file.EndsWith(".pdb", StringComparison.OrdinalIgnoreCase) ||
                    file.EndsWith(".cache", StringComparison.OrdinalIgnoreCase) ||
                    file.EndsWith(".ico", StringComparison.OrdinalIgnoreCase) ||
                    file.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string relativePath = Path.GetRelativePath(repositoryRoot, file).Replace('\\', '/');
                if (excludeRelativePaths.Contains(relativePath))
                {
                    continue;
                }

                try
                {
                    string content = File.ReadAllText(file);
                    if (content.Contains(legacyName, StringComparison.OrdinalIgnoreCase))
                    {
                        violations.Add(relativePath);
                    }
                }
                catch
                {
                    // Skip unreadable files
                }
            }
        }

        Assert.Empty(violations);
    }

    private static string GetRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "global.json")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the repository root.");
    }
}

