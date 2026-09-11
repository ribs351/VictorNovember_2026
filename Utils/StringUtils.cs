using System.Text.RegularExpressions;

namespace VictorNovember.Utils;

public static class StringUtils
{
    public static string FormatUptime(TimeSpan t)
    {
        if (t.TotalDays >= 1)
            return $"{(int)t.TotalDays}d {t.Hours}h {t.Minutes}m {t.Seconds}s";
        if (t.TotalHours >= 1)
            return $"{t.Hours}h {t.Minutes}m {t.Seconds}s";
        if (t.TotalMinutes >= 1)
            return $"{t.Minutes}m {t.Seconds}s";
        return $"{t.Seconds}s";
    }
    public static List<string> ProcessLLMOutput(string input)
    {
        const int limit = 1900;
        const int maxChars = 6000;

        var finalAnswer = /*ExtractFinalAnswer(input);*/ input;

        if (string.IsNullOrWhiteSpace(finalAnswer))
            return new List<string> { "(empty response)" };

        if (finalAnswer.Length > maxChars)
            finalAnswer = finalAnswer.Substring(0, maxChars) + "\n\n(…cut off)";

        finalAnswer = finalAnswer.Replace("\r\n", "\n").Trim();

        // Prevent mass pings
        finalAnswer = finalAnswer.Replace("@everyone", "@\u200Beveryone")
                   .Replace("@here", "@\u200Bhere");

        finalAnswer = Regex.Replace(finalAnswer, @"<@!?\d+>", m => m.Value.Insert(1, "\u200B"));
        finalAnswer = Regex.Replace(finalAnswer, @"<@&\d+>", m => m.Value.Insert(1, "\u200B"));
        finalAnswer = Regex.Replace(finalAnswer, @"<#\d+>", m => m.Value.Insert(1, "\u200B"));

        var chunks = new List<string>();

        while (!string.IsNullOrEmpty(finalAnswer))
        {
            if (finalAnswer.Length <= limit)
            {
                chunks.Add(finalAnswer);
                break;
            }

            int splitIndex = FindBestSplitIndex(finalAnswer, limit);

            var chunk = finalAnswer.Substring(0, splitIndex).TrimEnd();
            finalAnswer = finalAnswer.Substring(splitIndex).TrimStart();

            // Handle unclosed code blocks
            if (HasUnclosedCodeBlock(chunk))
            {
                chunk += "\n```";
                finalAnswer = "```\n" + finalAnswer;
            }

            chunks.Add(chunk);
        }

        return chunks;
    }
    private static int FindBestSplitIndex(string text, int limit)
    {
        var candidate = text.Substring(0, limit);

        // Paragraph break
        int index = candidate.LastIndexOf("\n\n", StringComparison.Ordinal);
        if (index > 0)
            return index + 2;

        // Line break
        index = candidate.LastIndexOf('\n');
        if (index > 0)
            return index + 1;

        // Sentence boundary
        index = candidate.LastIndexOf(". ");
        if (index > 0)
            return index + 2;

        // Hard split
        return limit;
    }
    private static bool HasUnclosedCodeBlock(string text)
    {
        int count = Regex.Matches(text, "```").Count;
        return count % 2 != 0;
    }

    private static string ExtractFinalAnswer(string responseText)
    {
        if (string.IsNullOrWhiteSpace(responseText))
            return string.Empty;

        // Use Singleline so '.' matches newlines inside the <final> block
        var match = Regex.Match(responseText, @"<final>(.*?)</final>", RegexOptions.Singleline | RegexOptions.IgnoreCase);

        if (match.Success)
        {
            return match.Groups[1].Value.Trim();
        }
        return responseText.Trim();
    }
}
