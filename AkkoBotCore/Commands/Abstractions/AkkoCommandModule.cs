using AkkoBot.Commands.Attributes;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;

namespace AkkoBot.Commands.Abstractions
{
    [IsNotBlacklisted, RequireBotPermissions(Permissions.SendMessages | Permissions.AddReactions)]
    public abstract class AkkoCommandModule : BaseCommandModule { }
}