using KokoroSharp;
using KokoroSharp.Core;
using KokoroSharp.Processing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using VictorNovember.Infrastructure.Models;
using VictorNovember.Interfaces;

namespace VictorNovember.Services.TTS;

public sealed class KokoroService : ITtsService
{
    private readonly ILogger<KokoroService> _logger;
    private readonly KokoroWavSynthesizer _synth;
    private readonly KokoroVoice _mixedVoice;

    public KokoroService(ILogger<KokoroService> logger, IConfiguration config)
    {
        _logger = logger;
        var voices = config.GetSection("KokoroOptions").Get<List<KokoroOptions>>() ?? throw new InvalidOperationException("KokoroOptions configuration is missing.");
        _logger.LogInformation("Loading Kokoro model...");
        _synth = KokoroWavSynthesizer.LoadModel();
        var mix = new List<(KokoroVoice voice, float weight)>();
        foreach (var item in voices)
        {
            var voice = GetVoice(item.VoiceName);
            if (voice != null) mix.Add((voice, item.Weight));
        }
        _mixedVoice = KokoroVoiceManager.Mix(mix.ToArray());

        _logger.LogInformation("Kokoro model loaded and voice mixed.");
    }
    public async Task<byte[]> SynthesizeAsync(string text, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation("Synthesizing...");
        var pipelineConfig = new KokoroTTSPipelineConfig
        {
            Speed = 0.9f,
            SecondsOfPauseBetweenProperSegments = new PauseAfterSegmentStrategy(
                CommaPause: 0.1f,
                PeriodPause: 0.5f,
                QuestionMarkPause: 0.7f,
                ExclamationMarkPause: 0.5f,
                NewLinePause: 0.6f,
                OthersPause: 0.5f
            )
        };

        var audioBytes = await _synth.SynthesizeAsync(text, _mixedVoice, pipelineConfig);
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
