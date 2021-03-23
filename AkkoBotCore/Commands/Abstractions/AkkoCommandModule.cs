using AkkoBot.Commands.Attributes;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace AkkoBot.Commands.Abstractions
{
    [IsNotBlacklisted, RequireBotPermissions(Permissions.SendMessages | Permissions.AddReactions)]
    public abstract class AkkoCommandModule : BaseCommandModule { }
}