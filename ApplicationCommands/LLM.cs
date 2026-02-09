using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using GenerativeAI.Exceptions;
using System.Net;
using System.Text.RegularExpressions;
using VictorNovember.Services;

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

            var chunks = ProcessLLMOutput(response);

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

    private static List<string> ProcessLLMOutput(string text)
    {
        const int limit = 1900; // safe margin under 2000

        const int maxChars = 6000;
        if (text.Length > maxChars)
            text = text.Substring(0, maxChars) + "\n\n(…cut off)";

        if (string.IsNullOrWhiteSpace(text))
            return new List<string> { "(empty response)" };
        text = text.Replace("\r\n", "\n").Trim();
        text = text.Replace("@everyone", "@\u200Beveryone")
               .Replace("@here", "@\u200Bhere");

        text = Regex.Replace(text, @"<@!?\d+>", m => m.Value.Insert(1, "\u200B"));
        text = Regex.Replace(text, @"<@&\d+>", m => m.Value.Insert(1, "\u200B"));
        text = Regex.Replace(text, @"<#\d+>", m => m.Value.Insert(1, "\u200B"));

        var chunks = new List<string>();
        for (int i = 0; i < text.Length; i += limit)
        {
            int len = Math.Min(limit, text.Length - i);
            chunks.Add(text.Substring(i, len));
        }

        return chunks;
    }
}
