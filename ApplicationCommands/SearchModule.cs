using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
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

            await ctx.DeleteResponseAsync();

            await ctx.Client
                .GetInteractivity()
                .SendPaginatedMessageAsync(
                    ctx.Channel,
                    ctx.User,
                    result.ToSearchPages(query, 5),
                    PaginationBehaviour.Ignore,
                    ButtonPaginationBehavior.Disable);
        }
        catch
        {
            // TODO: log properly later 
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("Something went wrong while searching."));
        }
    }
}
