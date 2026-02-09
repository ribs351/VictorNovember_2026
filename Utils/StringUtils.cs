using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace VictorNovember.Utils;

public sealed class StringUtils
{
    public static List<string> ProcessLLMOutput(string text)
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
