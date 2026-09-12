using VictorNovember.Services.NASA.Models;

namespace VictorNovember.Interfaces;

public interface INeoService
{
    Task<NeoResult> GetClosestApproachTodayAsync(CancellationToken ct = default);
    Task<string> GenerateCommentaryAsync(NeoResult neo);
}