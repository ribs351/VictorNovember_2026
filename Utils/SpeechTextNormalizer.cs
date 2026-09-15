using NMeCab;
using System.Text;

namespace VictorNovember.Utils;
public static class SpeechTextNormalizer
{
    // Katakana -> Hepburn-ish romaji map, longest-match-first.
    // This covers standard kana, yōon (contracted sounds), and extended katakana for loanwords.
    private static readonly (string Kana, string Romaji)[] KanaMap = new[]
    {
        // 3-char combos (yōon with extended vowels used in loanwords) - check these first
        ("ウォ","wo"), ("シェ","she"), ("ジェ","je"), ("チェ","che"),
        ("ティ","ti"), ("ディ","di"), ("デュ","dyu"), ("トゥ","tu"), ("ドゥ","du"),
        ("ファ","fa"), ("フィ","fi"), ("フェ","fe"), ("フォ","fo"), ("フュ","fyu"),
        ("ヴァ","va"), ("ヴィ","vi"), ("ヴェ","ve"), ("ヴォ","vo"), ("ヴ","vu"),

        // Standard yōon (きゃ, しゅ, etc.)
        ("キャ","kya"), ("キュ","kyu"), ("キョ","kyo"),
        ("ギャ","gya"), ("ギュ","gyu"), ("ギョ","gyo"),
        ("シャ","sha"), ("シュ","shu"), ("ショ","sho"),
        ("ジャ","ja"), ("ジュ","ju"), ("ジョ","jo"),
        ("チャ","cha"), ("チュ","chu"), ("チョ","cho"),
        ("ニャ","nya"), ("ニュ","nyu"), ("ニョ","nyo"),
        ("ヒャ","hya"), ("ヒュ","hyu"), ("ヒョ","hyo"),
        ("ビャ","bya"), ("ビュ","byu"), ("ビョ","byo"),
        ("ピャ","pya"), ("ピュ","pyu"), ("ピョ","pyo"),
        ("ミャ","mya"), ("ミュ","myu"), ("ミョ","myo"),
        ("リャ","rya"), ("リュ","ryu"), ("リョ","ryo"),

        // Basic gojuon
        ("ア","a"), ("イ","i"), ("ウ","u"), ("エ","e"), ("オ","o"),
        ("カ","ka"), ("キ","ki"), ("ク","ku"), ("ケ","ke"), ("コ","ko"),
        ("ガ","ga"), ("ギ","gi"), ("グ","gu"), ("ゲ","ge"), ("ゴ","go"),
        ("サ","sa"), ("シ","shi"), ("ス","su"), ("セ","se"), ("ソ","so"),
        ("ザ","za"), ("ジ","ji"), ("ズ","zu"), ("ゼ","ze"), ("ゾ","zo"),
        ("タ","ta"), ("チ","chi"), ("ツ","tsu"), ("テ","te"), ("ト","to"),
        ("ダ","da"), ("ヂ","ji"), ("ヅ","zu"), ("デ","de"), ("ド","do"),
        ("ナ","na"), ("ニ","ni"), ("ヌ","nu"), ("ネ","ne"), ("ノ","no"),
        ("ハ","ha"), ("ヒ","hi"), ("フ","fu"), ("ヘ","he"), ("ホ","ho"),
        ("バ","ba"), ("ビ","bi"), ("ブ","bu"), ("ベ","be"), ("ボ","bo"),
        ("パ","pa"), ("ピ","pi"), ("プ","pu"), ("ペ","pe"), ("ポ","po"),
        ("マ","ma"), ("ミ","mi"), ("ム","mu"), ("メ","me"), ("モ","mo"),
        ("ヤ","ya"), ("ユ","yu"), ("ヨ","yo"),
        ("ラ","ra"), ("リ","ri"), ("ル","ru"), ("レ","re"), ("ロ","ro"),
        ("ワ","wa"), ("ヲ","o"), ("ン","n"),
        ("ー",""), // long vowel mark handled via duplication step below; map here as fallback
    };

    private static readonly Dictionary<string, string> KanaDict =
        KanaMap.OrderByDescending(k => k.Kana.Length)
               .ToDictionary(k => k.Kana, k => k.Romaji);

    private static readonly MeCabTagger Tagger = MeCabTagger.Create();

    /// <summary>
    /// Converts Japanese text (kanji/kana, mixed with ASCII) into romaji
    /// suitable for feeding to an English-phoneme TTS engine like Kokoro.
    /// </summary>
    public static string ToRomaji(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        var sb = new StringBuilder();
        var nodes = Tagger.Parse(text);

        foreach (var node in nodes)
        {
            if (node.Stat == MeCabNodeStat.Bos || node.Stat == MeCabNodeStat.Eos)
                continue;

            var surface = node.Surface;
            var reading = ExtractReading(node);

            if (string.IsNullOrEmpty(reading))
            {
                sb.Append(surface);
                sb.Append(' ');
                continue;
            }

            sb.Append(KatakanaToRomaji(reading));
            sb.Append(' ');
        }

        return sb.ToString().Trim();
    }

    private static IEnumerable<MeCabNode> ParseNodes(MeCabNode head)
    {
        for (var node = head; node != null; node = node.Next)
        {
            if (node.Stat == MeCabNodeStat.Bos || node.Stat == MeCabNodeStat.Eos)
                continue;
            yield return node;
        }
    }

    private static string? ExtractReading(MeCabNode node)
    {
        // IPADIC feature string is CSV: pos,pos1,pos2,pos3,conj1,conj2,base,reading,pronunciation
        // Index 7 (0-based) is typically the Yomi (reading) field for known words.
        var features = node.Feature?.Split(',');
        if (features == null || features.Length < 8)
            return null;

        var reading = features[7];
        return reading == "*" ? null : reading;
    }

    private static string KatakanaToRomaji(string katakana)
    {
        var result = new StringBuilder();
        int i = 0;

        while (i < katakana.Length)
        {
            // Handle long vowel mark: duplicate previous vowel sound
            if (katakana[i] == 'ー' && result.Length > 0)
            {
                var lastVowel = result[result.Length - 1];
                if ("aeiou".IndexOf(lastVowel) >= 0)
                    result.Append(lastVowel);
                i++;
                continue;
            }

            // Handle small tsu (っ) - doubles the following consonant
            if (katakana[i] == 'ッ' && i + 1 < katakana.Length)
            {
                var nextRomaji = MatchLongestKana(katakana, i + 1, out var consumed);
                if (!string.IsNullOrEmpty(nextRomaji))
                {
                    result.Append(nextRomaji[0]); // double the leading consonant
                }
                i++;
                continue;
            }

            var romaji = MatchLongestKana(katakana, i, out var len);
            if (romaji != null)
            {
                result.Append(romaji);
                i += len;
            }
            else
            {
                result.Append(katakana[i]); // fallback: pass through unknown char
                i++;
            }
        }

        return result.ToString();
    }

    private static string? MatchLongestKana(string s, int start, out int consumed)
    {
        for (int len = 3; len >= 1; len--)
        {
            if (start + len > s.Length) continue;
            var sub = s.Substring(start, len);
            if (KanaDict.TryGetValue(sub, out var romaji))
            {
                consumed = len;
                return romaji;
            }
        }
        consumed = 1;
        return null;
    }
}