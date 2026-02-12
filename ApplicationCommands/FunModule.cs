using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using VictorNovember.Extensions;
using static VictorNovember.Enums.FunModuleEnums;

namespace VictorNovember.ApplicationCommands;

public sealed class FunModule : ApplicationCommandModule
{

    [SlashCommand("coinflip", "Flips a coin")]
    [SlashCooldown(1, 10, SlashCooldownBucketType.Channel)]
    public async Task CoinFlipAsync(InteractionContext ctx)
    {
        await ctx.DeferAsync();

        var result = (CoinSide)Random.Shared.Next(2);
        var resultText = result.ToString().ToLower();

        await ctx.EditResponseAsync(new DiscordWebhookBuilder()
            .WithContent($"{ctx.User.Mention} flipped a coin and got **{resultText}**!"));
    }

    #region RPS

    public static class RpsEngine
    {
        public static RpsOutcome Decide(RpsChoice player, RpsChoice november)
        {
            if (player == november)
                return RpsOutcome.Draw;

            return (player, november) switch
            {
                (RpsChoice.Rock, RpsChoice.Scissors) => RpsOutcome.PlayerWin,
                (RpsChoice.Scissors, RpsChoice.Paper) => RpsOutcome.PlayerWin,
                (RpsChoice.Paper, RpsChoice.Rock) => RpsOutcome.PlayerWin,
                _ => RpsOutcome.NovemberWin
            };
        }
    }

    private static readonly string[] NovemberWonLines =
    {
        "I've won! Hah!",
        "You've done well to lose against me.",
        "Outplayed! Don't feel bad, I'm just that great y'know?"
    };

    private static readonly string[] NovemberLostLines =
    {
        "Aww man, I lost!",
        "Dammit!",
        "One more go, I'll get it next time!"
    };
    private static readonly string[] DrawLines =
    {
        "Great minds think alike.",
        "A stalemate!",
        "We are evenly matched."
    };

    [SlashCommand("rps", "Play a Rock, Paper, Scissors with November")]
    [SlashCooldown(1, 10, SlashCooldownBucketType.Channel)]
    public async Task RockPaperScissorsAsync(
        InteractionContext ctx, 
        [Choice("Rock", "rock")]
        [Choice("Paper", "paper")]
        [Choice("Scissors", "scissors")]
        [Option("string", "Choose your weapon")] string choice)
    {
        var playerChoice = choice switch
        {
            "rock" => RpsChoice.Rock,
            "paper" => RpsChoice.Paper,
            "scissors" => RpsChoice.Scissors,
            _ => throw new InvalidOperationException("Invalid RPS choice")
        };

        var novemberChoice = (RpsChoice)Random.Shared.Next(0, 3);

        await ctx.DeferAsync();

        var outcome = RpsEngine.Decide(playerChoice, novemberChoice);
        var response = outcome switch
        {
            RpsOutcome.Draw => Random.Shared.PickRandom(DrawLines),
            RpsOutcome.PlayerWin => Random.Shared.PickRandom(NovemberWonLines),
            RpsOutcome.NovemberWin => Random.Shared.PickRandom(NovemberLostLines),
            _ => "Unhandled outcome",
        };

        var outcomeHeader = outcome switch
        {
            RpsOutcome.Draw => "It's a draw!",
            RpsOutcome.PlayerWin => "You won!",
            RpsOutcome.NovemberWin => "You lost!",
            _ => "Match result unknown."
        };

        await ctx.EditResponseAsync(new DiscordWebhookBuilder()
            .WithContent($"You threw **{playerChoice.ToDisplayString()}**, I threw **{novemberChoice.ToDisplayString()}**.\n\n**{outcomeHeader}**\n{response}"));
    }
    #endregion
    [SlashCommand("rr", "Play a game of Russian Roulette")]
    [SlashCooldown(1, 10, SlashCooldownBucketType.Channel)]
    public async Task RussianRouletteAsync(
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
        
        if (ctx.Channel.IsPrivate)
        {
            // if channel is a DM, then say no
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("This command can only be used in a server, where the stakes are present."));
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
            .WithContent($"The chamber was loaded! **{ctx.User.Mention}** shot themself in the head!"));
    }

}
