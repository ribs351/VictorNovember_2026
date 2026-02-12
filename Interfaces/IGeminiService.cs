using static VictorNovember.Enums.GeminiServiceEnums;

namespace VictorNovember.Interfaces;

public interface IGeminiService
{
    Task<string> GenerateAsync(string query, PromptMode mode = PromptMode.General, CancellationToken cancellationToken = default);
}
