using DSharpPlus.SlashCommands;
using Microsoft.Extensions.Configuration;

namespace VictorNovember.Utils;

public static class OwnerHelper
{
    public static bool IsOwner(InteractionContext ctx, IConfiguration config)
    {
        var ownerId = config["Discord:OwnerId"];

        if (string.IsNullOrWhiteSpace(ownerId))
            return false;

        return ulong.TryParse(ownerId, out var id) && ctx.User.Id == id;
    }
}
