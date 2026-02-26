using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using VictorNovember.Services.BraveSearch.Models;

namespace VictorNovember.Extensions;

public static class PaginationExtensions
{
    public static IEnumerable<Page> ToSearchPages(
        this SearchResult searchResult,
        string query,
        int resultsPerPage = 5)
    {
        var items = searchResult.Items;

        if (items.Count == 0)
            yield break;

        var totalPages = (int)Math.Ceiling(items.Count / (double)resultsPerPage);

        for (int i = 0; i < items.Count; i += resultsPerPage)
        {
            var chunk = items
                .Skip(i)
                .Take(resultsPerPage);

            var embed = new DiscordEmbedBuilder()
                .WithTitle($"Search Results for \"{query}\"")
                .WithColor(DiscordColor.Blurple)
                .WithFooter($"Page {(i / resultsPerPage) + 1}/{totalPages}");

            foreach (var item in chunk)
            {
                embed.AddField(
                    item.Title,
                    $"{Trim(item.Description)}\n[Open Link]({item.Url})");
            }

            yield return new Page(embed: embed);
        }
    }

    private static string Trim(string? text, int max = 300)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "No description.";

        return text.Length <= max
            ? text
            : text[..max] + "...";
    }
}