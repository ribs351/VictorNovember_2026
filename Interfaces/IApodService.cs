using VictorNovember.Services.NASA.Models;

namespace VictorNovember.Interfaces;

public interface IApodService
{
    Task<ApodResult> GetApodDataAsync(CancellationToken ct = default);
    Task<string> GenerateCommentaryAsync(string title, string explanation, string apodDate);
}
