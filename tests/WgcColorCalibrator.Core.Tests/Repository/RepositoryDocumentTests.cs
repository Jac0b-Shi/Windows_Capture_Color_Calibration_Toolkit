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

