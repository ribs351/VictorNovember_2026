using static VictorNovember.Enums.GeminiServiceEnums;

namespace VictorNovember.Interfaces;

public interface IGeminiService
{
    Task<string> GenerateTextAsync(string query, PromptMode mode = PromptMode.General, CancellationToken cancellationToken = default);
    Task<string> GenerateVisionCommentaryAsync(string imageUrl, string caption, CancellationToken cancellationToken = default);
}
