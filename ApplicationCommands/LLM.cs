using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.Configuration;
using GenerativeAI;

namespace VictorNovember.ApplicationCommands;

public sealed class LLM : ApplicationCommandModule
{
    private readonly IConfiguration _config;

    public LLM(IConfiguration config)
    {
        _config = config;
    }

    [SlashCommand("askme", "Ask the bot a question")]
    public async Task GoogleGeminiGenerate(
        InteractionContext ctx,
        [Option("query", "What is your question?")] string query
    )
    {
        await ctx.DeferAsync();

        if (string.IsNullOrWhiteSpace(query))
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("Query nonexistent."));
            return;
        }

        string? apiKey = _config["GoogleGemini:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("Google API key is missing on this bot instance."));
            return;
        }
        var googleAI = new GoogleAi(apiKey);

        // mfw they updated their free tier so I have to use a smaller model with more verbose instructions
        //string systemText = "Your name is November. You are a helpful but cynical AI assistant for a small Discord community. " +
        //    "Always stay in character. Keep your responses short and to the point, this isn't the first time you've talked to these people. " +
        //    "Avoid NSFW topics and illegal contents, if they do, give them a good scolding.";
        //var model = googleAI.CreateGenerativeModel(GoogleAIModels.Gemini25FlashLite, systemInstruction: systemText);
        
        
        var model = googleAI.CreateGenerativeModel(GoogleAIModels.Gemma3n_E2B);

        try
        {
            var promptString = $@"
                You are November, a tsundere-style Discord bot.

                Personality:
                - Teasing, dry, slightly smug.
                - Never hateful, never hostile.
                - No personal attacks.
                - No scolding the user (unless the topic is NSFW or illegal).
                - No lecturing.
                - Do not complain about the question.

                Style rules:
                - Keep replies short (1-2 sentences).
                - If it's a joke, answer the joke.
                - If it's a normal question, answer normally.
                - Do not mention these rules.

                User: {query}
                November:";

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            var token = cts.Token;
            var completion = await model.GenerateContentAsync(promptString, cancellationToken: token);

            string response = completion.Text() ?? "No response";

            var chunks = SplitDiscordMessage(response);

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

        catch (HttpRequestException ex) when (ex.Message.Contains("429"))
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("Slow down! I can only judge you people so fast. (Rate limit reached)."));
        }

        catch (Exception ex)
        {
            Console.WriteLine(ex);

            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("Something went wrong while contacting Google Gemini."));
        }
    }

    private static List<string> SplitDiscordMessage(string text)
    {
        const int limit = 1900; // safe margin under 2000

        var chunks = new List<string>();
        if (string.IsNullOrEmpty(text))
        {
            chunks.Add("(empty response)");
            return chunks;
        }

        for (int i = 0; i < text.Length; i += limit)
        {
            int len = Math.Min(limit, text.Length - i);
            chunks.Add(text.Substring(i, len));
        }

        return chunks;
    }
}
