using static VictorNovember.Enums.GeminiServiceEnums;

namespace VictorNovember.Interfaces;

public interface IPromptProviderService
{
    string GetBasePrompt();
    string GetModeInstructions(PromptMode mode);
}
