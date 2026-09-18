using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using VictorNovember.Extensions;
using VictorNovember.Interfaces;

namespace VictorNovember.ApplicationCommands;

public sealed class SearchModule : ApplicationCommandModule
{
    private readonly ISearchService _searchService;

    public SearchModule(ISearchService searchService)
    {
        _searchService = searchService;
    }

    [SlashCommand("search", "Search the web using November")]
    public async Task SearchAsync(
        InteractionContext ctx,
        [Option("query", "What do you want to search for?")]
        string query)
    {
        await ctx.DeferAsync();

        try
        {
            var result = await _searchService.SearchWebAsync(query, CancellationToken.None);

            if (result.Items.Count == 0)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("No results found."));
                return;
            }

            var pages = result.ToSearchPages(query, 5).ToList();

            await ctx.SendPaginatedMessageWithJumpAsync(pages, ctx.User);
        }
        catch
        {
            // TODO: log properly later 
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("Something went wrong while searching."));
        }
    }
}
