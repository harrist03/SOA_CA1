using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using SOA_CA1.Services;

namespace SOA_CA1.Tests;

public class HoldingsRepositoryTests
{
    [Fact]
    public async Task LoadAsync_WhenSeedFileExists_ReturnsPortfolio()
    {
        using var temporaryDirectory = TemporaryDirectory.Create();
        var seedDirectory = Directory.CreateDirectory(Path.Combine(temporaryDirectory.Path, "Data"));
        await File.WriteAllTextAsync(
            Path.Combine(seedDirectory.FullName, "holdings.json"),
            """
            {
              "name": "Test Portfolio",
              "dateCreated": "2026-10-01T10:00:00Z",
              "holdings": [
                {
                  "id": "3f2c0e5a-8a41-4c57-9d3e-1b7a6c2f4e10",
                  "symbol": "AAPL",
                  "name": "Apple Inc.",
                  "quantity": 10,
                  "buyPrice": 187.50,
                  "dateBought": "2026-09-03"
                }
              ]
            }
            """);

        var portfolio = await CreateRepository(temporaryDirectory.Path).LoadAsync();

        var holding = Assert.Single(portfolio.Holdings);
        Assert.Equal("Test Portfolio", portfolio.Name);
        Assert.Equal("AAPL", holding.Symbol);
        Assert.Equal(10m, holding.Quantity);
        Assert.Equal(187.50m, holding.BuyPrice);
    }

    [Fact]
    public async Task LoadAsync_WhenSeedFileIsMissing_ReturnsEmptyPortfolio()
    {
        using var temporaryDirectory = TemporaryDirectory.Create();

        var portfolio = await CreateRepository(temporaryDirectory.Path).LoadAsync();

        Assert.Empty(portfolio.Holdings);
        Assert.Equal("My Portfolio", portfolio.Name);
    }

    [Fact]
    public async Task LoadAsync_WhenSeedFileIsCorrupt_ThrowsWithoutOverwritingFile()
    {
        using var temporaryDirectory = TemporaryDirectory.Create();
        var seedDirectory = Directory.CreateDirectory(Path.Combine(temporaryDirectory.Path, "Data"));
        var filePath = Path.Combine(seedDirectory.FullName, "holdings.json");
        const string corruptJson = "{ invalid json";
        await File.WriteAllTextAsync(filePath, corruptJson);

        var exception = await Assert.ThrowsAsync<InvalidDataException>(
            () => CreateRepository(temporaryDirectory.Path).LoadAsync());

        Assert.Contains("invalid JSON", exception.Message);
        Assert.Equal(corruptJson, await File.ReadAllTextAsync(filePath));
    }

    private static HoldingsRepository CreateRepository(string contentRootPath)
    {
        return new HoldingsRepository(new TestHostEnvironment(contentRootPath));
    }

    private sealed class TestHostEnvironment(string contentRootPath) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Test";
        public string ApplicationName { get; set; } = "SOA_CA1.Tests";
        public string ContentRootPath { get; set; } = contentRootPath;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        private TemporaryDirectory(string path)
        {
            Path = path;
        }

        public string Path { get; }

        public static TemporaryDirectory Create()
        {
            return new TemporaryDirectory(Directory.CreateTempSubdirectory("soa-ca1-tests-").FullName);
        }

        public void Dispose()
        {
            Directory.Delete(Path, recursive: true);
        }
    }
}
