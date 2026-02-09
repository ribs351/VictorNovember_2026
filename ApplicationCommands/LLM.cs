using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using GenerativeAI.Exceptions;
using System.Text.RegularExpressions;
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
            var token = cts.Token;
            var response = await _gemini.GenerateAsync(query, cts.Token);

            if (string.IsNullOrWhiteSpace(response))
                response = "No response.";

            var chunks = StringUtils.ProcessLLMOutput(response);

            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent(chunks[0]));

            // If more chunks, send follow-up messages
            for (int i = 1; i < chunks.Count; i++)
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                    .WithContent(chunks[i]));
            }
        }
        catch (OperationCanceledException)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("Gemini took too long and timed out. Try again."));
        }

        catch (ApiException ex) when (ex.Message.Contains("Code: 429"))
        {
            if (ex.Message.Contains("Quota exceeded", StringComparison.OrdinalIgnoreCase))
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("I’m out of Gemini quota right now. Try again later."));
                return;
            }

            double? retrySeconds = null;

            var match = Regex.Match(ex.Message, @"retry in ([0-9]+(\.[0-9]+)?)s", RegexOptions.IgnoreCase);
            if (match.Success && double.TryParse(match.Groups[1].Value, out var seconds))
                retrySeconds = seconds;

            var msg = retrySeconds is not null
                ? $"Slowdown! Try again in ~{Math.Ceiling(retrySeconds.Value)}s."
                : "Slowdown! Try again in a bit.";

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(msg));
        }

        catch (Exception ex)
        {
            Console.WriteLine(ex);

            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("Something went wrong while contacting Google Gemini."));
        }
    }

    
}
