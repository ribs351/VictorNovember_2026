using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using VictorNovember.Interfaces;
using VictorNovember.Utils;

namespace VictorNovember.ApplicationCommands;

public sealed class MemorialModule : ApplicationCommandModule
{
    private readonly IMemorialService _memorialService;
    private readonly IConfiguration _config;
    private readonly ILogger<MemorialModule> _logger;

    public MemorialModule(
        IMemorialService memorialService,
        IConfiguration config,
        ILogger<MemorialModule> logger)
    {
        _memorialService = memorialService;
        _config = config;
        _logger = logger;
    }

    [SlashCommand("memorial-add", "Add a memorial")]
    public async Task AddMemorialAsync(
        InteractionContext ctx,
        [Option("name", "Name of the person")] string name,
        [Option("message", "Message to send on their anniversary")] string message,
        [Option("month", "Anniversary month")] long month,
        [Option("day", "Anniversary day")] long day)
    {
        await ctx.DeferAsync(ephemeral: true);

        if (!OwnerHelper.IsOwner(ctx, _config))
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("You don't have permission to use this command."));
            return;
        }

        try
        {
            if (month is < 1 or > 12 || day is < 1 or > 31)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("Invalid month or day."));
                return;
            }

            DateOnly date;
            try
            {
                date = new DateOnly(DateTime.UtcNow.Year, (int)month, (int)day);
            }
            catch
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("Invalid date combination."));
                return;
            }


            var memorial = await _memorialService.AddMemorialAsync(
                name,
                message,
                ctx.User.Id,
                date);

            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent($"Memorial for **{memorial.PersonName}** has been registered. They will not be forgotten."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add memorial for {Name}", name);
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("Something went wrong adding the memorial."));
        }
    }

    [SlashCommand("memorial-list", "List all memorials")]
    public async Task ListMemorialsAsync(InteractionContext ctx)
    {
        await ctx.DeferAsync(ephemeral: true);

        if (!OwnerHelper.IsOwner(ctx, _config))
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("You don't have permission to use this command."));
            return;
        }

        var memorials = await _memorialService.GetAllMemorialsAsync();

        if (memorials.Count == 0)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("No memorials registered."));
            return;
        }

        var embed = new DiscordEmbedBuilder()
            .WithTitle("Memorials")
            .WithColor(DiscordColor.Purple);

        foreach (var memorial in memorials)
        {
            embed.AddField(
                memorial.PersonName,
                $"Date: {memorial.Date:MMMM dd}\nMessage: {memorial.Message}\nID: `{memorial.Id}`");
        }

        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
    }

    [SlashCommand("memorial-remove", "Remove a memorial")]
    public async Task RemoveMemorialAsync(
        InteractionContext ctx,
        [Option("id", "Memorial ID to remove")] string id)
    {
        await ctx.DeferAsync(ephemeral: true);

        if (!OwnerHelper.IsOwner(ctx, _config))
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("You don't have permission to use this command."));
            return;
        }

        if (!Guid.TryParse(id, out var guid))
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("Invalid ID format."));
            return;
        }

        var removed = await _memorialService.RemoveMemorialAsync(guid);

        await ctx.EditResponseAsync(new DiscordWebhookBuilder()
            .WithContent(removed
                ? "Memorial removed."
                : "No memorial found with that ID."));
    }
}