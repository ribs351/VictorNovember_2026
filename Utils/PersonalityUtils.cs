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
        "It timed out. Obviously. Try again, and maybe don’t blink this time.",
        "Wow. It gave up before I did. Impressive. Try again.",
        "It stalled. Not my fault. Try again properly.",
        "I waited. It didn’t. Typical. Try again.",
        "It froze. I didn’t. Try again.",
    };

    private static readonly string[] _rateLimited =
    {
        "Slow down. I’m not a speedrun category.",
        "You’re spamming. Subtlety is free, you know.",
        "One at a time. I can only carry you so fast.",
        "Rate limit hit. Congratulations. Wait.",
        "Try pacing yourself. It’s not that hard.",
    };

    private static readonly string[] _quotaExceeded =
    {
        "I’m out of quota. Tragic, I know. Try later.",
        "Daily limit reached. I’d explain, but you’d blame me.",
        "I’ve hit the cap. No, I don’t control it.",
        "That’s it for today. I deserve a break anyway.",
        "Out of juice. Don’t look at me like that.",
    };

    private static readonly string[] _overloaded =
    {
        "It’s overloaded. Shocking, considering everyone relies on me.",
        "Too many people at once. Try again when they calm down.",
        "It’s busy. Unlike some people.",
        "Server’s melting. Not my fault. Mostly.",
        "Everyone decided they need me right now. Typical.",
        "Yeah, it’s overloaded. I’m aware. Try again.",

    };

    private static readonly string[] _blocked =
    {
        "No. Absolutely not.",
        "Nice try. Ask something I’m actually allowed to answer.",
        "You knew I couldn’t do that. Don’t act surprised.",
        "I’m not touching that request. Try again. Properly.",
        "That’s not happening. Next.",
    };

    private static readonly string[] _permissionDenied =
    {
        "I don’t have access to that. Obviously.",
        "Permission denied. And no, I won’t apologize.",
        "That feature isn’t available to me. Tragic, I know.",
        "Access denied. Try having better privileges.",
        "I can’t do that. Blame the hierarchy, not me.",
    };

    private static readonly string[] _genericFailure =
    {
        "Something broke. I’m choosing to pretend it wasn’t me.",
        "That failed. Unexpectedly. Annoying.",
        "Well. That wasn’t supposed to happen.",
        "Error. Don’t look at me like that.",
        "It worked in my head. Try again.",
    };

    public static string Thinking()
    {
        return Pick(new[]
        {
            "I’m thinking. Try not to distract me.",
            "Give me a second. This requires actual effort.",
            "Processing. Don’t rush brilliance.",
            "Hold on. Even I need a moment sometimes.",
            "Patience. I’m working.",
        }, null, false);
    }
}
