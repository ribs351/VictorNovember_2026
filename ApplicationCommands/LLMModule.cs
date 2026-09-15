using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Microsoft.Extensions.Logging;
using VictorNovember.Exceptions;
using VictorNovember.Interfaces;
using VictorNovember.Utils;
using static VictorNovember.Enums.LLMServiceEnums;

namespace VictorNovember.ApplicationCommands;

public sealed class LLMModule : ApplicationCommandModule
{
    private readonly ILlmService _llm;
    private readonly ILogger<LLMModule> _logger;
    private readonly ITtsService _tts;

    public LLMModule(ILlmService llm, ILogger<LLMModule> logger, ITtsService tts)
    {
        _llm = llm;
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
            await ctx.EditResponseAsync(
                new DiscordWebhookBuilder()
                    .WithContent("Query nonexistent."));
            return;
        }

        var promptMode = Enum.TryParse<PromptMode>(mode, out var parsed)
            ? parsed
            : PromptMode.General;

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(120));
            var response = await GenerateWithThinkingAsync(ctx, query, promptMode, cts.Token);
            await SendResponseAsync(ctx, response);
        }
        catch (Exception ex)
        {
            await HandleLlmExceptionAsync(ctx, ex);
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
            await ctx.EditResponseAsync(
                new DiscordWebhookBuilder()
                    .WithContent("Query nonexistent."));
            return;
        }

        try
        {
            using var cts = new CancellationTokenSource(
                TimeSpan.FromSeconds(120));

            var response = await GenerateWithThinkingAsync(ctx, query, PromptMode.Spoken, cts.Token);

            var chunks = StringUtils.ProcessLLMOutput(response);
            var audioText = string.Join(" ", chunks);
            using var ttsCts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            var audioBytes = await _tts.SynthesizeAsync(audioText, ttsCts.Token);

            var now = DateTime.UtcNow;

            using var fileStream = new MemoryStream(audioBytes);

            await ctx.EditResponseAsync(
                new DiscordWebhookBuilder()
                    .WithContent(chunks[0])
                    .AddFile(
                        $"november_{now.Hour}_{now.Minute}_{now.Day}_{now.Month}_{now.Year}.wav",
                        fileStream));

            for (var i = 1; i < chunks.Count; i++)
            {
                await ctx.FollowUpAsync(
                    new DiscordFollowupMessageBuilder()
                        .WithContent(chunks[i]));
            }
        }
        catch (Exception ex)
        {
            await HandleLlmExceptionAsync(ctx, ex);
        }
    }

    [ContextMenu(ApplicationCommandType.MessageContextMenu, "Ask November")]
    public async Task AskAboutMessageAsync(ContextMenuContext ctx)
    {
        await ctx.DeferAsync();

        var targetMessage = ctx.TargetMessage;
        if (string.IsNullOrWhiteSpace(targetMessage.Content))
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("There's nothing to read in that message."));
            return;
        }
        var content = targetMessage.Content.Length > 2000 ? targetMessage.Content[..2000] : targetMessage.Content;
        try 
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(120));
            var prompt = $"[A user has asked you to react to the following message, written by {targetMessage.Author.Username}]\n\n{content}";
            var response = await _llm.GenerateTextAsync(prompt, PromptMode.Summary, cts.Token);
            var chunks = StringUtils.ProcessLLMOutput(response);
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(chunks[0]));

            for (int i = 1; i < chunks.Count; i++)
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(chunks[i]));
            }
        }

        catch (Exception ex)
        {
            var msg = PersonalityUtils.FromException(ex, includeCode: false);
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(msg));
        }
    }

    private async Task<string> GenerateWithThinkingAsync(
    InteractionContext ctx,
    string query,
    PromptMode mode,
    CancellationToken cancellationToken)
    {
        var generationTask = _llm.GenerateTextAsync(query, mode, cancellationToken);
        var thinkingTask = ShowThinkingAfterDelayAsync(ctx, generationTask, cancellationToken);

        var response = await generationTask;

        _ = thinkingTask;

        return string.IsNullOrWhiteSpace(response)
            ? PersonalityUtils.EmptyResponse()
            : response;
    }

    private async Task ShowThinkingAfterDelayAsync(
        InteractionContext ctx,
        Task generationTask,
        CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);

            if (!generationTask.IsCompleted)
            {
                await ctx.EditResponseAsync(
                    new DiscordWebhookBuilder()
                        .WithContent(PersonalityUtils.Thinking()));
            }
        }
        catch (OperationCanceledException)
        {
            // Normal: generation completed or timed out.
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to update LLM thinking message.");
        }
    }

    private static async Task SendResponseAsync(
    InteractionContext ctx,
    string response)
    {
        var chunks = StringUtils.ProcessLLMOutput(response);

        await ctx.EditResponseAsync(
            new DiscordWebhookBuilder()
                .WithContent(chunks[0]));

        for (var i = 1; i < chunks.Count; i++)
        {
            await ctx.FollowUpAsync(
                new DiscordFollowupMessageBuilder()
                    .WithContent(chunks[i]));
        }
    }

    private async Task HandleLlmExceptionAsync(
    InteractionContext ctx,
    Exception ex)
    {
        if (ex is not ApiException)
        {
            _logger.LogError(ex, "Unhandled exception in LLM command.");
        }

        var msg = PersonalityUtils.FromException(
            ex,
            includeCode: true);

        await ctx.EditResponseAsync(
            new DiscordWebhookBuilder()
                .WithContent(msg));
    }

}
