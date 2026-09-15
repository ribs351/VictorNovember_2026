using KokoroSharp;
using KokoroSharp.Core;
using KokoroSharp.Processing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using VictorNovember.Infrastructure.Models;
using VictorNovember.Interfaces;
using VictorNovember.Utils;

namespace VictorNovember.Services.TTS;

public sealed class KokoroService : ITtsService
{
    private readonly ILogger<KokoroService> _logger;
    private readonly KokoroWavSynthesizer _synth;
    private readonly Dictionary<string, KokoroVoice> _voiceMixes = new();
    private readonly KokoroPipelineOptions _pipelineOptions;

    public KokoroService(ILogger<KokoroService> logger, IConfiguration config, IOptions<KokoroMultiLanguageOptions> voiceOptions, IOptions<KokoroPipelineOptions> pipelineOptions)
    {
        _logger = logger;
        _pipelineOptions = pipelineOptions.Value;
        
        _logger.LogInformation("Loading Kokoro model...");
        _synth = KokoroWavSynthesizer.LoadModel();
        _voiceMixes["English"] = MixVoices(voiceOptions.Value.English);
        _voiceMixes["Japanese"] = MixVoices(voiceOptions.Value.Japanese);

        _logger.LogInformation("Kokoro model loaded and voice mixed.");
    }
    private KokoroVoice MixVoices(List<KokoroVoiceConfig> configs)
    {
        if (configs is null || configs.Count == 0)
            throw new InvalidOperationException("Voice mix configuration is empty or missing.");

        var mix = new List<(KokoroVoice voice, float weight)>();
        foreach (var config in configs)
        {
            var voice = KokoroVoiceManager.GetVoice(config.VoiceName);
            if (voice != null)
                mix.Add((voice, config.Weight));
            else
                _logger.LogWarning("Voice '{VoiceName}' not found in KokoroVoiceManager, skipping.", config.VoiceName);
        }

        if (mix.Count == 0)
            throw new InvalidOperationException("No valid voices resolved for this language's mix.");

        return KokoroVoiceManager.Mix(mix.ToArray());
    }

    public async Task<byte[]> SynthesizeAsync(string text, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        var language = LanguageDetector.Detect(text);

        if (!_voiceMixes.TryGetValue(language, out var voiceMix))
        {
            _logger.LogWarning("Language {Language} not found, defaulting to English.", language);
            voiceMix = _voiceMixes["English"];
            language = "English";
        }

        var ttsText = language switch
        {
            "Japanese" => SpeechTextNormalizer.ToRomaji(text),
            _ => text
        };

        _logger.LogInformation("Synthesizing...");
        var pipelineConfig = new KokoroTTSPipelineConfig
        {
            Speed = _pipelineOptions.Speed,
            SecondsOfPauseBetweenProperSegments = new PauseAfterSegmentStrategy(
                CommaPause: _pipelineOptions.CommaPause,
                PeriodPause: _pipelineOptions.PeriodPause,
                QuestionMarkPause: _pipelineOptions.QuestionMarkPause,
                ExclamationMarkPause: _pipelineOptions.ExclamationMarkPause,
                NewLinePause: _pipelineOptions.NewLinePause,
                OthersPause: _pipelineOptions.OthersPause
            )
        };


        var audioBytes = await _synth.SynthesizeAsync(ttsText, voiceMix, pipelineConfig);
        stopwatch.Stop();
        _logger.LogInformation("Synthesis completed in {Elapsed}ms", stopwatch.ElapsedMilliseconds);
        var tempFile = Path.GetTempFileName();
        try 
        {
            KokoroWavSynthesizer.SaveAudioToFile(audioBytes, tempFile);
            var bytes = await File.ReadAllBytesAsync(tempFile, cancellationToken);
            return bytes;
        }
        finally 
        {
            File.Delete(tempFile);
        }
    }

    private KokoroVoice GetVoice(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Input has no voice name.");
        }

        var voice = KokoroVoiceManager.GetVoice(name);
        if (voice is null)
        {
            throw new ArgumentException($"Voice '{name}' not found.");
        }

        return voice;
    }
}
