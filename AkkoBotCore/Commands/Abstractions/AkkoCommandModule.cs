using AkkoBot.Commands.Attributes;
using DSharpPlus;
using DSharpPlus.CommandsNext;

namespace AkkoBot.Commands.Abstractions
{
    [IsNotBlacklisted, BaseBotPermissions(Permissions.SendMessages | Permissions.AddReactions)]
    public abstract class AkkoCommandModule : BaseCommandModule { }
}