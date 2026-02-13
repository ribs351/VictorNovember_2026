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
    public static List<string> ProcessLLMOutput(string text)
    {
        const int limit = 1900;
        const int maxChars = 6000;

        if (string.IsNullOrWhiteSpace(text))
            return new List<string> { "(empty response)" };

        if (text.Length > maxChars)
            text = text.Substring(0, maxChars) + "\n\n(…cut off)";

        text = text.Replace("\r\n", "\n").Trim();

        // Prevent mass pings
        text = text.Replace("@everyone", "@\u200Beveryone")
                   .Replace("@here", "@\u200Bhere");

        text = Regex.Replace(text, @"<@!?\d+>", m => m.Value.Insert(1, "\u200B"));
        text = Regex.Replace(text, @"<@&\d+>", m => m.Value.Insert(1, "\u200B"));
        text = Regex.Replace(text, @"<#\d+>", m => m.Value.Insert(1, "\u200B"));

        var chunks = new List<string>();

        while (!string.IsNullOrEmpty(text))
        {
            if (text.Length <= limit)
            {
                chunks.Add(text);
                break;
            }

            int splitIndex = FindBestSplitIndex(text, limit);

            var chunk = text.Substring(0, splitIndex).TrimEnd();
            text = text.Substring(splitIndex).TrimStart();

            // Handle unclosed code blocks
            if (HasUnclosedCodeBlock(chunk))
            {
                chunk += "\n```";
                text = "```\n" + text;
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

}
