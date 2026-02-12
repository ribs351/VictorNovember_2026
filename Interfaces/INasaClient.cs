using VictorNovember.Infrastructure.Models;

namespace VictorNovember.Interfaces;

public interface INasaClient
{
    Task<ApodResponse> GetApodAsync(DateTime? date = null, CancellationToken ct = default);
    Task<MarsRoverResponse> GetMarsPhotosAsync(CancellationToken ct = default);
}
