using Microsoft.Extensions.Hosting;
using VictorNovember.Interfaces;
using static VictorNovember.Enums.GeminiServiceEnums;

namespace VictorNovember.Services;

public sealed class FilePromptProviderService : IPromptProviderService
{
    private readonly string _basePrompt;
    private readonly Dictionary<PromptMode, string> _modePrompts;

    public FilePromptProviderService(IHostEnvironment env)
    {
        var promptPath = Path.Combine(env.ContentRootPath, "Prompts");
        _basePrompt = File.ReadAllText(
            Path.Combine(promptPath, "BasePersonality.txt"));

        _modePrompts = new()
        {
            { PromptMode.General, File.ReadAllText(Path.Combine(promptPath, "General.txt")) },
            { PromptMode.InformativeReaction, File.ReadAllText(Path.Combine(promptPath, "InformativeReaction.txt")) },
            { PromptMode.Technical, File.ReadAllText(Path.Combine(promptPath, "Technical.txt")) },
            { PromptMode.Detailed, File.ReadAllText(Path.Combine(promptPath, "Detailed.txt")) }
        };
    }

    public string GetBasePrompt() => _basePrompt;
    public string GetModeInstructions(PromptMode mode)
        => _modePrompts.TryGetValue(mode, out var prompt)
            ? prompt
            : string.Empty;
}
