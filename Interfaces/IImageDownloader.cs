namespace VictorNovember.Interfaces;

public interface IImageDownloader
{
    Task<string> DownloadToTempFileAsync(string imageUrl, CancellationToken ct = default);
}
