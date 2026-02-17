using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;
using VictorNovember.Utils;

namespace VictorNovember.Jobs;

public sealed class MemorialSigil
{
    private readonly DiscordClientProvider _clientProvider;
    private readonly ILogger<MemorialSigil> _logger;
    public MemorialSigil(ILogger<MemorialSigil> logger, DiscordClientProvider clientProvider)
    {
        _logger = logger;
        _clientProvider = clientProvider;
    }

    public async Task ExecuteAsync(string personName, string message, ulong recipientUserId)
    {
        _logger.LogInformation($"MemorialSigil firing for {personName}");

        var client = _clientProvider.Client;
        try 
        {
            var user = await client.GetUserAsync(recipientUserId);

            if (user is not DiscordMember member)
            {
                // Irrelevant in practice, I'll always be in the same guild with the bot, but this is to get rid of the cast warning
                _logger.LogWarning($"MemorialSigil: Could not resolve {recipientUserId} as DiscordMember");
                return;
            }

            var dm = await member.CreateDmChannelAsync();
            var embed = new DiscordEmbedBuilder()
                            .WithTitle($"In Memory of {personName}")
                            .WithDescription(message)
                            .WithColor(new DiscordColor(148, 163, 184))
                            .WithFooter("Lest we forget.")
                            .WithTimestamp(DateTimeOffset.UtcNow);

            await dm.SendMessageAsync(new DiscordMessageBuilder().AddEmbed(embed));
            _logger.LogInformation($"MemorialSigil delivered for {personName}");
        }
        catch 
        {
            _logger.LogInformation("Failed to send DM to recipient.");
        }
    }
}
