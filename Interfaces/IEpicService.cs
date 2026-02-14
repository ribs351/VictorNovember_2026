using VictorNovember.Services.NASA.Models;

namespace VictorNovember.Interfaces;

public interface IEpicService
{
    Task<EarthImage> GetRandomEarthImageAsync(CancellationToken ct = default);
    Task<string> GenerateCommentary(EarthImage earthImage, CancellationToken ct = default);
}
