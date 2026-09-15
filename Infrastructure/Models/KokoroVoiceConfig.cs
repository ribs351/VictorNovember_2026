namespace VictorNovember.Infrastructure.Models;

public sealed class KokoroVoiceConfig
{
    public string VoiceName { get; set; } = string.Empty;
    public float Weight { get; set; }
}

public class KokoroMultiLanguageOptions
{
    public List<KokoroVoiceConfig> English { get; set; } = new();
    public List<KokoroVoiceConfig> Japanese { get; set; } = new();
    //public List<KokoroVoiceConfig> Chinese { get; set; } = new();
}

public sealed class KokoroPipelineOptions
{
    public float Speed { get; set; } = 1.1f;
    public float CommaPause { get; set; } = 0.1f;
    public float PeriodPause { get; set; } = 0.5f;
    public float QuestionMarkPause { get; set; } = 0.7f;
    public float ExclamationMarkPause { get; set; } = 0.5f;
    public float NewLinePause { get; set; } = 0.6f;
    public float OthersPause { get; set; } = 0.5f;
}