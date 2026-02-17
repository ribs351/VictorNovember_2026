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
        _logger.LogInformation("MemorialSigil firing for {PersonName}", personName);

        var client = _clientProvider.Client;
        try 
        {
            var user = await client.GetUserAsync(recipientUserId);

            if (user is not DiscordMember member)
            {
                // Irrelevant in practice, I'll always be in the same guild with the bot, but this is to get rid of the cast warning
                _logger.LogWarning("MemorialSigil: Could not resolve {UserId} as DiscordMember", recipientUserId);
                return;
            }

            var dm = await member.CreateDmChannelAsync();
            await dm.SendMessageAsync(message);
            _logger.LogInformation("MemorialSigil delivered for {PersonName}", personName);
        }
        catch 
        {
            _logger.LogInformation("Failed to send DM to recipient.");
        }
    }
}
