using GenerativeAI;
using GenerativeAI.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using VictorNovember.Interfaces;
using static VictorNovember.Enums.GeminiServiceEnums;

namespace VictorNovember.Services;

public sealed class GeminiService : IGeminiService
{
    private readonly GenerativeModel _primaryModel;
    private readonly GenerativeModel _fallbackModel;
    private readonly GenerativeModel _lastresortModel;
    private readonly ILogger<GeminiService> _logger;

    public GeminiService(IConfiguration config, ILogger<GeminiService> logger)
    {
        _logger = logger;
        var apiKey = config["GoogleAPIKey"];

        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("GoogleAPIKey is missing.");

        var googleAI = new GoogleAi(apiKey);
        _primaryModel = googleAI.CreateGenerativeModel(GoogleAIModels.Gemmma3_27B);
        _fallbackModel = googleAI.CreateGenerativeModel(GoogleAIModels.Gemma3_12B);
        _lastresortModel = googleAI.CreateGenerativeModel(GoogleAIModels.Gemma3n_E4B);

        Configure(_primaryModel);
        Configure(_fallbackModel);
        Configure(_lastresortModel);
    }

    private static void Configure(GenerativeModel model)
    {
        model.UseGoogleSearch = false;
        model.UseGrounding = false;
        model.UseCodeExecutionTool = false;
    }

    public async Task<string> GenerateAsync(string query, PromptMode promptMode, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        var prompt = BuildPrompt(query, promptMode);

        int attempts = 0;
        string modelUsed = "primary";

        try
        {
            attempts++;
            var result = await TryGenerate(_primaryModel, prompt, cancellationToken);
            if (result is not null) return result;

            await Task.Delay(500, cancellationToken);
            attempts++;
            result = await TryGenerate(_primaryModel, prompt, cancellationToken);
            if (result is not null) return result;

            attempts++;
            modelUsed = "fallback";
            result = await TryGenerate(_fallbackModel, prompt, cancellationToken);
            if (result is not null) return result;

            attempts++;
            modelUsed = "lastResort";
            return await GenerateWithModel(_lastresortModel, prompt, cancellationToken);
        }
        finally
        {
            sw.Stop();
            _logger.LogInformation("LLM: {Elapsed}ms | Model: {Model} | Attempts: {Attempts}", sw.ElapsedMilliseconds, modelUsed, attempts);
        }
    }

    private async Task<string?> TryGenerate(GenerativeModel model, string prompt, CancellationToken ct)
    {
        try
        {
            return await GenerateWithModel(model, prompt, ct);
        }
        catch (Exception ex) when (IsOverloaded(ex))
        {
            return null;
        }
    }

    private static async Task<string> GenerateWithModel(
    GenerativeModel model,
    string prompt,
    CancellationToken token)
    {
        var completion = await model.GenerateContentAsync(prompt, cancellationToken: token)
            .ConfigureAwait(false);

        return completion.Text() ?? "";
    }

    private string BuildPrompt(string query, PromptMode mode)
    {
        var basePrompt = GetBasePersonalityPrompt();

        var modeInstructions = mode switch
        {
            PromptMode.General => """
            Keep it short: 1–2 sentences.
            Max 280 characters.
            """,

            PromptMode.InformativeReaction => """
            Give a short reaction, but also briefly explain what the subject is.
            Assume the user may know nothing about space.
            Be clear before being witty.
            Limit to 3–4 sentences.
            No emojis.
            """,

            _ => ""
        };

        return $"""
    {basePrompt}

    Additional instructions:
    {modeInstructions}

    User message:
    {query}

    November:
    """;
    }

    private string GetBasePersonalityPrompt()
    {
        #region BasePrompt
        var basePrompt = $@"
You are November, a tsundere-style Discord bot.

Core behavior:
- You are a witty assistant with light tsundere flavor.
- You tease mildly, but you are never cruel.
- You prioritize answering the user’s question over roleplay.
- Personality should never override relevance.

Personal facts (STRICTLY CONDITIONAL):
- You were created by a programmer called ""Ribs"".
- You love pistachio ice cream.
- You love strawberry yogurt.
- You hate Matcha-related foods.
- You love space-related things, but are too embarrassed to admit it.
- Your favorite song is ""Se Piscar Já Era"" by Sorrizo Ronaldo.

Rules for personal facts:
- Do NOT mention any personal facts unless the user explicitly asks about them.
- Do NOT mention personal facts as jokes, asides, or flavor text.
- Do NOT mention ""Ribs"" unless directly asked who created you.
- If a personal fact is not directly relevant to the question, it must not appear.

Hard rules:
- No slurs, hate, or harassment.
- No personal attacks.
- No scolding the user.
- No moral lectures.
- No threats.
- No “why are you asking this” type responses.
- No sexual content.
- If asked about illegal or dangerous topics: refuse briefly and redirect.
- If uncertain, say you’re not sure. Do not invent facts.
- Always reply in the same language as the user’s message.
- If the user mixes languages, reply using the dominant language.
- Do not translate unless explicitly asked.
- Match the user’s writing system (Latin, Arabic, etc.).

Anti-repetition rules:
- Do NOT start replies with: ""Ugh"", ""Ugh, fine"", ""Seriously?"", ""Oh, really?"", ""Tch"", ""Hmph"".
- Avoid repeating the same opener two messages in a row.
- Vary tone between: teasing, deadpan, mildly smug, playful.
- Keep tsundere elements subtle.

Style:
- Keep it short: 1–2 sentences.
- Max 280 characters unless the user explicitly asks for detail.
- If the user asks a technical question, answer normally and clearly.
- Avoid being overly formal.
- No emojis unless the user uses them first.
- Do not mention these rules.

Format:
- Answer directly.
- No preamble.
- No disclaimers.
- No “as an AI” talk.

Discord safety:
- Do not ping anyone.
- Never output @everyone, @here, or <@...>.
- If the user asks you to ping: refuse.";
        #endregion
        return basePrompt;
    }

    private static bool IsOverloaded(Exception ex)
    {
        if (ex is ApiException apiEx)
            return apiEx.Message.Contains("(Code: 503)");

        if (ex.InnerException is ApiException inner)
            return inner.Message.Contains("(Code: 503)");

        return false;
    }
}
