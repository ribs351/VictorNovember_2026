using GenerativeAI.Exceptions;
using System.Text.RegularExpressions;
using System.Threading;

namespace VictorNovember.Utils;

public static class PersonalityUtils
{
    private static readonly Random _rng = new();

    public static string FromException(Exception ex, bool includeCode = false)
    {
        if (ex is OperationCanceledException)
            return Timeout();

        if (ex is ApiException apiEx)
            return FromApiException(apiEx, includeCode);

        if (ex.InnerException is ApiException innerApiEx)
            return FromApiException(innerApiEx, includeCode);

        return GenericFailure();
    }

    public static string EmptyResponse() => "… I got nothing. (Empty response)";

    public static string FromApiException(ApiException ex, bool includeCode = false)
    {
        var code = TryExtractApiCode(ex.Message);
        var retrySeconds = TryExtractRetrySeconds(ex.Message);

        if (IsQuotaExceeded(ex.Message))
            return QuotaExceeded(code, includeCode);

        // 429: rate limit
        if (code == "429")
            return RateLimited(retrySeconds, code, includeCode);

        // 503: overloaded / high demand
        if (code == "503")
            return Overloaded(code, includeCode);

        // 400-ish: blocked / invalid request
        if (ex.Message.Contains("SAFETY", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("blocked", StringComparison.OrdinalIgnoreCase))
            return Blocked(code, includeCode);

        // 401/403: key missing, forbidden, etc
        if (code == "401" || code == "403")
            return PermissionDenied(code, includeCode);

        return GenericFailure(code, includeCode);
    }

    public static string Timeout(string? code = null, bool includeCode = false)
        => Pick(_timeout, code, includeCode);

    public static string RateLimited(double? retrySeconds = null, string? code = null, bool includeCode = false)
    {
        if (retrySeconds is not null)
        {
            var secs = (int)Math.Ceiling(retrySeconds.Value);
            return Pick(new[]
            {
                $"Slow down. Try again in ~{secs}s.",
                $"Too fast. Give it ~{secs}s and try again.",
                $"I’m being throttled. Try again in ~{secs}s.",
            }, code, includeCode);
        }

        return Pick(_rateLimited, code, includeCode);
    }

    public static string QuotaExceeded(string? code = null, bool includeCode = false)
        => Pick(_quotaExceeded, code, includeCode);

    public static string Overloaded(string? code = null, bool includeCode = false)
        => Pick(_overloaded, code, includeCode);

    public static string Blocked(string? code = null, bool includeCode = false)
        => Pick(_blocked, code, includeCode);

    public static string PermissionDenied(string? code = null, bool includeCode = false)
        => Pick(_permissionDenied, code, includeCode);

    public static string GenericFailure(string? code = null, bool includeCode = false)
        => Pick(_genericFailure, code, includeCode);

    private static string Pick(IReadOnlyList<string> options, string? code, bool includeCode)
    {
        if (options.Count == 0)
            return "Something went wrong.";

        var msg = options[_rng.Next(options.Count)];

        if (includeCode && !string.IsNullOrWhiteSpace(code))
            msg += $" (err: {code})";

        return msg;
    }

    private static bool IsQuotaExceeded(string message)
        => message.Contains("Quota exceeded", StringComparison.OrdinalIgnoreCase)
        || message.Contains("current quota", StringComparison.OrdinalIgnoreCase);

    private static string? TryExtractApiCode(string message)
    {
        var match = Regex.Match(message, @"\(Code:\s*(\d+)\)");
        return match.Success ? match.Groups[1].Value : null;
    }

    private static double? TryExtractRetrySeconds(string message)
    {
        var match = Regex.Match(message, @"retry in\s+([0-9]+(\.[0-9]+)?)s", RegexOptions.IgnoreCase);
        if (!match.Success)
            return null;

        if (double.TryParse(match.Groups[1].Value, out var seconds))
            return seconds;

        return null;
    }

    private static readonly string[] _timeout =
    {
        "That took too long. Try again.",
        "Nope, timed out. Try again in a moment.",
        "I waited. It didn’t answer. Try again.",
        "It’s thinking *really* hard. Too hard. Try again.",
    };

    private static readonly string[] _rateLimited =
    {
        "Slow down. Try again in a bit.",
        "Too fast! Try again in a moment.",
        "Give it a second. Then try again.",
        "Rate limit hit. Try again shortly.",
    };

    private static readonly string[] _quotaExceeded =
    {
        "I’m out of juice right now. Try again later.",
        "I can’t run any more requests right now. Try later.",
        "I’ve hit my daily limit. Try again later.",
        "I’m capped for now. Try again later.",
    };

    private static readonly string[] _overloaded =
    {
        "Servers are overloaded right now. Try again soon.",
        "It’s getting hammered right now. Try again in a minute.",
        "High demand. Try again shortly.",
        "It’s busy right now. Try again soon.",
        "Everyone decided to talk at once. Try again in a bit.",
        "Yeah, no — it’s overloaded. Give it a minute.",

    };

    private static readonly string[] _blocked =
    {
        "Nope. I can’t help with that.",
        "Nice try. Not doing that one.",
        "I’m not allowed to answer that request.",
        "Yeahhh… no. Ask something else.",
    };

    private static readonly string[] _permissionDenied =
    {
        "I’m not allowed to do that right now.",
        "I don’t have access to that capability.",
        "That feature isn’t available to me right now.",
        "Can’t do that — access denied.",
    };

    private static readonly string[] _genericFailure =
    {
        "Something went wrong. Try again later.",
        "That didn’t work. Try again.",
        "It broke. Not my fault. (Okay maybe a little.) Try again.",
        "Unexpected error. Try again later.",
    };
}
