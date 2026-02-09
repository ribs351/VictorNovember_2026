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
- If the user asks you to ping: refuse.

User message:
{query}

November:
";
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
