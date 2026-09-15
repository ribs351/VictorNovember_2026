namespace VictorNovember.Utils;

public static class LanguageDetector
{
    public static string Detect(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "English";

        int japaneseCount = 0;
        int totalLetters = 0;

        foreach (var ch in text)
        {
            if (IsJapanese(ch))
                japaneseCount++;

            if (char.IsLetter(ch))
                totalLetters++;
        }

        if (totalLetters == 0)
            return "English";

        double ratio = (double)japaneseCount / totalLetters;
        return ratio > 0.15 ? "Japanese" : "English";
    }

    private static bool IsJapanese(char c)
    {
        return (c >= '\u3040' && c <= '\u309F')  // Hiragana
            || (c >= '\u30A0' && c <= '\u30FF')  // Katakana
            || (c >= '\u4E00' && c <= '\u9FFF')  // CJK Unified Ideographs (kanji)
            || (c >= '\u3400' && c <= '\u4DBF'); // CJK Extension A
    }
}