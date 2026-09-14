using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Microsoft.Extensions.Logging;
using VictorNovember.Interfaces;
using VictorNovember.Utils;
using VictorNovember.Exceptions;
using static VictorNovember.Enums.LLMServiceEnums;

namespace VictorNovember.ApplicationCommands;

public sealed class LLMModule : ApplicationCommandModule
{
    private readonly ILlmService _llm;
    private readonly ILogger<LLMModule> _logger;
    private readonly ITtsService _tts;

    public LLMModule(ILlmService lmm, ILogger<LLMModule> logger, ITtsService tts)
    {
        _llm = lmm;
        _logger = logger;
        _tts = tts;
    }

    [SlashCommand("llm", "Converse with the bot")]
    [SlashCooldown(1, 10, SlashCooldownBucketType.User)]
    [SlashCooldown(1, 5, SlashCooldownBucketType.Global)]
    public async Task LLMGenerateText(
        InteractionContext ctx,
        [Option("query", "What do you want to talk about? (may take a moment to respond)")] string query,
        [Choice("General", "General")]
        [Choice("InformativeReaction", "InformativeReaction")]
        [Choice("Technical", "Technical")]
        [Choice("Detailed", "Detailed")]
        [Option("mode", "Adjust November's answer style")]
        string mode = "General"
    )
    {
        await ctx.DeferAsync();

        if (string.IsNullOrWhiteSpace(query))
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("Query nonexistent."));
            return;
        }

        var promptMode = Enum.TryParse<PromptMode>(mode, out var parsed)
            ? parsed
            : PromptMode.General;

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(120));
            var generationTask = _llm.GenerateTextAsync(query, promptMode, cts.Token);

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(10), cts.Token);

                    if (!generationTask.IsCompleted)
                    {
                        await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                            .WithContent(PersonalityUtils.Thinking()));
                    }
                }
                catch (OperationCanceledException)
                {
                    // generation finished or request cancelled
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message);
                }
            });

            var response = await generationTask;

            if (string.IsNullOrWhiteSpace(response))
                response = PersonalityUtils.EmptyResponse();

            var chunks = StringUtils.ProcessLLMOutput(response);

            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent(chunks[0]));

            for (int i = 1; i < chunks.Count; i++)
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                    .WithContent(chunks[i]));
            }
        }
        
        catch (Exception ex)
        {
            if (ex is not ApiException)
                Console.WriteLine(ex);

            var msg = PersonalityUtils.FromException(ex, includeCode: true);

            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent(msg));
        }
    }

    [SlashCommand("llm-tts", "Converse with the bot, with text-to-speech appended to the message (beta)")]
    [SlashCooldown(1, 10, SlashCooldownBucketType.User)]
    [SlashCooldown(1, 5, SlashCooldownBucketType.Global)]
    public async Task LLMGenerateTTS(
        InteractionContext ctx,
        [Option("query", "What do you want to talk about? (may take a moment to respond)")] string query
    )
    {
        await ctx.DeferAsync();

        if (string.IsNullOrWhiteSpace(query))
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("Query nonexistent."));
            return;
        }

        var promptMode = PromptMode.Spoken;

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(120));
            var generationTask = _llm.GenerateTextAsync(query, promptMode, cts.Token);

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(10), cts.Token);

                    if (!generationTask.IsCompleted)
                    {
                        await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                            .WithContent(PersonalityUtils.Thinking()));
                    }
                }
                catch (OperationCanceledException)
                {
                    // generation finished or request cancelled
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message);
                }
            });

            var response = await generationTask;

            if (string.IsNullOrWhiteSpace(response))
                response = PersonalityUtils.EmptyResponse();

            var now = DateTime.UtcNow;

            var chunks = StringUtils.ProcessLLMOutput(response);
            var audioText = string.Join(" ", chunks);
            var audioBytes = await _tts.SynthesizeAsync(audioText, cts.Token);

            using var fileStream = new MemoryStream(audioBytes);

            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent(chunks[0])
                .AddFile($"november_{now.Hour}_{now.Minute}_{now.Day}_{now.Month}_{now.Year}.wav", fileStream));

            for (int i = 1; i < chunks.Count; i++)
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                    .WithContent(chunks[i]));
            }
        }

        catch (Exception ex)
        {
            if (ex is not ApiException)
                Console.WriteLine(ex);

            var msg = PersonalityUtils.FromException(ex, includeCode: true);

            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent(msg));
        }
    }

}
