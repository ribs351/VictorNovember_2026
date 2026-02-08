using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;

namespace VictorNovember.ApplicationCommands;

public sealed class Fun : ApplicationCommandModule
{
    [SlashCommand("rr", "Play a game of Russian Roulette")]
    [SlashCooldown(1, 10, SlashCooldownBucketType.Channel)]
    public async Task RussianRoulette(
        InteractionContext ctx,
        [Choice("One", 1)]
        [Choice("Two", 2)]
        [Choice("Three", 3)]
        [Choice("Four", 4)]
        [Choice("Five", 5)]
        [Choice("Six", 6)]
        [Option("bullets", "How many bullets do you want to load?")] long bulletsInput
    )
    {
        await ctx.DeferAsync();
        if (ctx.Guild is null || ctx.Member is null)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("This command can only be used in a server, where the stakes are present."));
            return;
        }

        int bullets = (int)bulletsInput;
        bullets = Math.Clamp(bullets, 1, 6);

        var member = ctx.Member;

        await ctx.EditResponseAsync(new DiscordWebhookBuilder()
            .WithContent($"{ctx.User.Mention} loads **{bullets}** bullet(s) and spins the cylinder..."));

        await Task.Delay(1000);

        await ctx.EditResponseAsync(new DiscordWebhookBuilder()
            .WithContent($"{ctx.User.Mention} puts the gun up to their head and pulls the trigger..."));

        await Task.Delay(1000);

        // Chance = bullets/6
        // bullets = 1 => 16%
        // bullets = 6 => 100%
        int deathChancePercent = (int)Math.Round((bullets / 6.0) * 100.0);
        int roll = Random.Shared.Next(100); // 0-99

        bool dies = roll < deathChancePercent;

        if (!dies)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("*click*"));
            await Task.Delay(500);

            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent($"The chamber was empty! **{ctx.User.Mention}** has survived!"));
            return;
        }

        await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("**BANG!**"));
        await Task.Delay(500);

        if (member.Hierarchy >= ctx.Guild.CurrentMember.Hierarchy)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent($"The gun fired! But it bounced off of **{ctx.User.Mention}**'s head! Their skull is just too thick."));
            return;
        }

        var until = DateTimeOffset.UtcNow.AddMinutes(2);

        try
        {
            await member.TimeoutAsync(until);
        }
        catch
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent($"The chamber was loaded! **{ctx.User.Mention}** should have been dead... but they dodged the bullet because don't have permission 😭"));
            return;
        }

        try
        {
            var dm = await member.CreateDmChannelAsync();
            await dm.SendMessageAsync(
                $"You've been timed out in **{ctx.Guild.Name}** for 2 minutes.\n" +
                $"You died in a game of Russian Roulette. Try again if you dare.");
        }
        catch
        {
            // ignore DM failure
        }

        await ctx.EditResponseAsync(new DiscordWebhookBuilder()
            .WithContent($"The chamber was loaded! **{ctx.User.Username}** shot themself in the head!"));
    }

}
