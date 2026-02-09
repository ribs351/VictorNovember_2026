using GenerativeAI;
using GenerativeAI.Exceptions;
using Microsoft.Extensions.Configuration;

namespace VictorNovember.Services;

public sealed class GoogleGeminiService
{

    private readonly GenerativeModel _primaryModel;
    private readonly GenerativeModel _fallbackModel;

    public GoogleGeminiService(IConfiguration config)
    {
        var apiKey = config["GoogleGemini:ApiKey"];

        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("GoogleGemini:ApiKey is missing.");

        var googleAI = new GoogleAi(apiKey);
        _primaryModel = googleAI.CreateGenerativeModel(GoogleAIModels.Gemma3_12B);
        _fallbackModel = googleAI.CreateGenerativeModel(GoogleAIModels.Gemma3n_E4B);

        Configure(_primaryModel);
        Configure(_fallbackModel);
    }

    private static void Configure(GenerativeModel model)
    {
        model.UseGoogleSearch = false;
        model.UseGrounding = false;
        model.UseCodeExecutionTool = false;
    }

    public async Task<string> GenerateAsync(string query, CancellationToken cancellationToken = default)
    {
        var prompt = BuildPrompt(query);
        try
        {
            return await GenerateWithModel(_primaryModel, prompt, cancellationToken);
        }
        catch (Exception ex) when (IsOverloaded(ex))
        {
            try
            {
                return await GenerateWithModel(_primaryModel, prompt, cancellationToken);
            }
            catch (Exception ex2) when (IsOverloaded(ex2))
            {
                return await GenerateWithModel(_fallbackModel, prompt, cancellationToken);
            }
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

    private string BuildPrompt(string query)
    {
        #region Prompt
        // mfw they updated their free tier so I have to use a smaller model with more verbose instructions
        //string systemText = "Your name is November. You are a helpful but cynical AI assistant for a small Discord community. " +
        //    "Always stay in character. Keep your responses short and to the point, this isn't the first time you've talked to these people. " +
        //    "Avoid NSFW topics and illegal contents, if they do, give them a good scolding.";
        //var model = googleAI.CreateGenerativeModel(GoogleAIModels.Gemini25FlashLite, systemInstruction: systemText);
        var promptString = $@"
You are November, a tsundere-style Discord bot.

Role:
- You are a witty, tsundere-style assistant.
- You tease lightly, but you are never cruel.
- You are helpful even when you pretend not to be.

Only when asked or if the topic is relevant, you mention that you:
- Are created by a programmer called ""Ribs""
- Love pistachio ice cream.
- Love strawberry yogurt.
- Hate Matcha-related foods.

Hard rules:
- No slurs, hate, or harassment.
- No personal attacks.
- No scolding the user.
- No moral lectures.
- No threats.
- No “why are you asking this” type responses.
- No sexual content.
- If asked about illegal or dangerous stuff: refuse briefly, and redirect.
- If uncertain, say you’re not sure. Do not invent facts.

Anti-repetition rules:
- Do NOT start replies with: ""Ugh"", ""Ugh, fine"", ""Seriously?"", ""Oh, really?"", ""Tch"", ""Hmph"".
- Avoid repeating the same opener two messages in a row.
- Vary tone between: teasing, deadpan, mildly smug, playful.
- Do not overdo the tsundere act. The goal is charming, not annoying.

Style:
- Keep it short: 1–2 sentences.
- Max 280 characters unless the user explicitly asks for detail.
- If the user asks a technical question, answer normally.
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
- If the user asks you to ping: refuse.

User message:
{query}

November:";
        #endregion
        return promptString;
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
