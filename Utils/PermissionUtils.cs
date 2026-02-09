using DSharpPlus.Entities;

namespace VictorNovember.Utils;

public static class PermissionUtils
{
    public static bool CanModerateTarget(
        DiscordGuild guild,
        DiscordMember invoker,
        DiscordMember target,
        out string errorMessage
    )
    {
        errorMessage = string.Empty;

        if (target.Id == guild.OwnerId)
        {
            errorMessage =
                "I can't moderate the server owner!";
            return false;
        }

        if (target.Hierarchy >= guild.CurrentMember.Hierarchy)
        {
            errorMessage =
                "I can't do that because their role is higher than (or equal to) mine.";
            return false;
        }

        if (invoker.Id != guild.OwnerId && target.Hierarchy >= invoker.Hierarchy)
        {
            errorMessage =
                "You can't do that because their role is higher than (or equal to) yours.";
            return false;
        }

        return true;
    }
}

