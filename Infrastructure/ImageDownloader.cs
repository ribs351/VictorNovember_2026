using VictorNovember.Interfaces;

namespace VictorNovember.Infrastructure;

public sealed class ImageDownloader : IImageDownloader
{
    // Unused right now, will find a way to squeeze this in later
    private readonly HttpClient _httpClient;

    public ImageDownloader(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> DownloadToTempFileAsync(
        string imageUrl,
        CancellationToken ct = default)
    {
        var tempFilePath = Path.Combine(
            Path.GetTempPath(),
            $"{Guid.NewGuid()}.png");

        await using var stream = await _httpClient.GetStreamAsync(imageUrl, ct);
        await using var fileStream = File.Create(tempFilePath);

        await stream.CopyToAsync(fileStream, ct);

        return tempFilePath;
    }
}
