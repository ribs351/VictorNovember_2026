using DSharpPlus;

namespace VictorNovember.Utils;

public sealed class DiscordClientProvider
{
    // This is used to grab the DiscordClient so it can be injected directly
    private DiscordClient? _client;

    public DiscordClient Client => _client
        ?? throw new InvalidOperationException("Discord client not initialized.");

    public void SetClient(DiscordClient client) => _client = client;
}
