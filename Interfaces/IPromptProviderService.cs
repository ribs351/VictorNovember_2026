using static VictorNovember.Enums.LLMServiceEnums;

namespace VictorNovember.Interfaces;

public interface IPromptProviderService
{
    string GetBasePrompt();
    string GetModeInstructions(PromptMode mode);
}
