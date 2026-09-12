using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;

namespace VictorNovember.Extensions;

/// <summary>
/// Custom paginator with prev/next/first/last buttons and a "Jump" button that opens a modal for typing a page number directly.
/// </summary>
public static class CustomPaginator
{
    private const string PrevId = "paginator_prev";
    private const string NextId = "paginator_next";
    private const string FirstId = "paginator_first";
    private const string LastId = "paginator_last";
    private const string JumpId = "paginator_jump";
    private const string JumpModalId = "paginator_jump_modal";
    private const string JumpModalInputId = "paginator_jump_input";

    public static async Task SendPaginatedMessageWithJumpAsync(
        this InteractionContext ctx,
        IReadOnlyList<Page> pages,
        DiscordUser requestingUser,
        TimeSpan? timeout = null)
    {
        if (pages.Count == 0)
            return;

        timeout ??= TimeSpan.FromMinutes(2);
        var currentIndex = 0;

        var message = await ctx.EditResponseAsync(BuildWebhookBuilder(pages[currentIndex], currentIndex, pages.Count));

        if (pages.Count == 1)
            return;

        var interactivity = ctx.Client.GetInteractivity();

        while (true)
        {
            var buttonResult = await interactivity.WaitForButtonAsync(
                message,
                requestingUser,
                timeout.Value);

            if (buttonResult.TimedOut)
            {
                await DisableButtonsAsync(ctx, message, pages[currentIndex], currentIndex, pages.Count);
                return;
            }

            var e = buttonResult.Result;

            switch (e.Id)
            {
                case PrevId:
                    currentIndex = Math.Max(0, currentIndex - 1);
                    await e.Interaction.CreateResponseAsync(
                        InteractionResponseType.UpdateMessage,
                        BuildInteractionBuilder(pages[currentIndex], currentIndex, pages.Count));
                    break;

                case NextId:
                    currentIndex = Math.Min(pages.Count - 1, currentIndex + 1);
                    await e.Interaction.CreateResponseAsync(
                        InteractionResponseType.UpdateMessage,
                        BuildInteractionBuilder(pages[currentIndex], currentIndex, pages.Count));
                    break;

                case FirstId:
                    currentIndex = 0;
                    await e.Interaction.CreateResponseAsync(
                        InteractionResponseType.UpdateMessage,
                        BuildInteractionBuilder(pages[currentIndex], currentIndex, pages.Count));
                    break;

                case LastId:
                    currentIndex = pages.Count - 1;
                    await e.Interaction.CreateResponseAsync(
                        InteractionResponseType.UpdateMessage,
                        BuildInteractionBuilder(pages[currentIndex], currentIndex, pages.Count));
                    break;

                case JumpId:
                    var handled = await HandleJumpAsync(e.Interaction, interactivity, pages.Count);
                    if (handled is int newIndex)
                    {
                        currentIndex = newIndex;
                        
                        await e.Message.ModifyAsync(new DiscordMessageBuilder()
                            .WithContent(string.Empty)
                            .AddEmbed(pages[currentIndex].Embed)
                            .AddComponents(BuildButtons(currentIndex, pages.Count)));
                    }
                    break;
            }
        }
    }

    private static async Task<int?> HandleJumpAsync(
        DiscordInteraction interaction,
        InteractivityExtension interactivity,
        int pageCount)
    {
        var modal = new DiscordInteractionResponseBuilder()
            .WithTitle("Jump to page")
            .WithCustomId(JumpModalId)
            .AddComponents(new TextInputComponent(
                label: $"Page number (1-{pageCount})",
                customId: JumpModalInputId,
                placeholder: "e.g. 3",
                min_length: 1,
                max_length: 4,
                required: true));

        await interaction.CreateResponseAsync(InteractionResponseType.Modal, modal);

        var modalResult = await interactivity.WaitForModalAsync(JumpModalId, TimeSpan.FromSeconds(60));

        if (modalResult.TimedOut)
            return null;

        var input = modalResult.Result.Values.GetValueOrDefault(JumpModalInputId);

        await modalResult.Result.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);

        if (!int.TryParse(input, out var requestedPage))
            return null;

        // User-facing pages are 1-indexed;
        var clamped = Math.Clamp(requestedPage - 1, 0, pageCount - 1);
        return clamped;
    }

    private static DiscordWebhookBuilder BuildWebhookBuilder(Page page, int index, int total) =>
        new DiscordWebhookBuilder()
            .AddEmbed(page.Embed)
            .AddComponents(BuildButtons(index, total));

    private static DiscordInteractionResponseBuilder BuildInteractionBuilder(Page page, int index, int total) =>
        new DiscordInteractionResponseBuilder()
            .AddEmbed(page.Embed)
            .AddComponents(BuildButtons(index, total));

    private static DiscordButtonComponent[] BuildButtons(int index, int total)
    {
        var isFirst = index == 0;
        var isLast = index == total - 1;

        return new[]
        {
            new DiscordButtonComponent(ButtonStyle.Secondary, FirstId, "\u00ab", disabled: isFirst),
            new DiscordButtonComponent(ButtonStyle.Secondary, PrevId, "\u2039 Prev", disabled: isFirst),
            new DiscordButtonComponent(ButtonStyle.Primary, JumpId, $"{index + 1}/{total}"),
            new DiscordButtonComponent(ButtonStyle.Secondary, NextId, "Next \u203a", disabled: isLast),
            new DiscordButtonComponent(ButtonStyle.Secondary, LastId, "\u00bb", disabled: isLast),
        };
    }

    private static async Task DisableButtonsAsync(
        InteractionContext ctx,
        DiscordMessage message,
        Page page,
        int index,
        int total)
    {
        var disabledButtons = BuildButtons(index, total)
            .Select(b => new DiscordButtonComponent(b.Style, b.CustomId, b.Label, disabled: true))
            .ToArray();

        await ctx.EditResponseAsync(new DiscordWebhookBuilder()
            .AddEmbed(page.Embed)
            .AddComponents(disabledButtons));
    }
}