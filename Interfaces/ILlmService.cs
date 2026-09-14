using static VictorNovember.Enums.LLMServiceEnums;

namespace VictorNovember.Interfaces;

public interface ILlmService
{
    Task<string> GenerateTextAsync(string query, PromptMode mode = PromptMode.General, CancellationToken cancellationToken = default);
    Task<string> GenerateVisionCommentaryAsync(string imageUrl, double? latitude, double? longitude, string caption, CancellationToken cancellationToken = default);
}
