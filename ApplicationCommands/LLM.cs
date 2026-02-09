using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using GenerativeAI.Exceptions;
using VictorNovember.Services;
using VictorNovember.Utils;

namespace VictorNovember.ApplicationCommands;

public sealed class LLM : ApplicationCommandModule
{
    private readonly GoogleGeminiService _gemini;

    public LLM(GoogleGeminiService gemini)
    {
        _gemini = gemini;
    }

    [SlashCommand("llm", "Converse with the bot")]
    [SlashCooldown(1, 10, SlashCooldownBucketType.User)]
    [SlashCooldown(1, 5, SlashCooldownBucketType.Global)]
    public async Task LLMGenerateText(
        InteractionContext ctx,
        [Option("query", "What do you want to talk about?")] string query
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

            var response = await _gemini.GenerateAsync(query, cts.Token);

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
