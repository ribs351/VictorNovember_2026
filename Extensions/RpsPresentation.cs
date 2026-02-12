using static VictorNovember.Enums.FunModuleEnums;

namespace VictorNovember.Extensions;

public static class RpsPresentation
{
    public static string ToEmoji(this RpsChoice choice) => choice switch
    {
        RpsChoice.Rock => "🪨",
        RpsChoice.Paper => "📄",
        RpsChoice.Scissors => "✂️",
        _ => "?"
    };

    public static string ToDisplayString(this RpsChoice choice)
        => $"{choice.ToEmoji()} {choice}";
}

