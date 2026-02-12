using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using GenerativeAI.Exceptions;
using Microsoft.Extensions.Logging;
using VictorNovember.Interfaces;
using VictorNovember.Utils;
using static VictorNovember.Enums.GeminiServiceEnums;

namespace VictorNovember.ApplicationCommands;

public sealed class LLMModule : ApplicationCommandModule
{
    private readonly IGeminiService _gemini;
    private readonly ILogger<LLMModule> _logger;

    public LLMModule(IGeminiService gemini, ILogger<LLMModule> logger)
    {
        _gemini = gemini;
        _logger = logger;
    }

    [SlashCommand("llm", "Converse with the bot")]
    [SlashCooldown(1, 10, SlashCooldownBucketType.User)]
    [SlashCooldown(1, 5, SlashCooldownBucketType.Global)]
    public async Task LLMGenerateText(
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

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            var generationTask = _gemini.GenerateAsync(query, PromptMode.General, cts.Token);

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

            var msg = PersonalityUtils.FromException(ex, includeCode: false);

            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent(msg));
        }
    }

}
