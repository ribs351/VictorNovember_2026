using VictorNovember.Infrastructure.Models;

namespace VictorNovember.Interfaces;

public interface IEpicClient
{
    Task<IReadOnlyList<EpicImage>> GetNaturalAsync(
        CancellationToken ct = default);
}
