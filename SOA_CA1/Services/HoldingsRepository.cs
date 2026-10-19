using System.Text.Json;
using SOA_CA1.Models;

namespace SOA_CA1.Services;

public sealed class HoldingsRepository : IHoldingsRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };
    private static readonly SemaphoreSlim FileLock = new(1, 1);

    private readonly string filePath;

    public HoldingsRepository(IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(environment);
        filePath = Path.Combine(environment.ContentRootPath, "Data", "holdings.json");
    }

    public async Task<Portfolio> LoadAsync()
    {
        if (!File.Exists(filePath))
        {
            return new Portfolio();
        }

        try
        {
            await using var stream = File.OpenRead(filePath);
            var portfolio = await JsonSerializer.DeserializeAsync<Portfolio>(stream, JsonOptions);
            return portfolio ?? throw new InvalidDataException("The holdings seed file does not contain a portfolio.");
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("The holdings seed file contains invalid JSON.", exception);
        }
    }

    public async Task SaveAsync(Portfolio portfolio)
    {
        ArgumentNullException.ThrowIfNull(portfolio);

        var directoryPath = Path.GetDirectoryName(filePath)
            ?? throw new InvalidOperationException("The holdings file directory could not be determined.");
        Directory.CreateDirectory(directoryPath);

        // Create unique temporary filename
        var temporaryFilePath = $"{filePath}.{Guid.NewGuid():N}.tmp";
        await FileLock.WaitAsync();
        try
        {
            // Write the complete JSON document before replacing the existing portfolio file.
            await using (var stream = new FileStream(
                temporaryFilePath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 4096,
                useAsync: true))
            {
                // Convert the portfolio object into formatted JSON and write it to the temporary file.
                await JsonSerializer.SerializeAsync(stream, portfolio, JsonOptions);
                await stream.FlushAsync();
            }

            // Replace the previous file only after the temporary JSON document is complete.
            File.Move(temporaryFilePath, filePath, overwrite: true);
        }
        finally
        {
            FileLock.Release();
            // Remove the temporary file if serialization or replacement failed.
            if (File.Exists(temporaryFilePath))
            {
                File.Delete(temporaryFilePath);
            }
        }
    }
}
