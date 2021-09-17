using AkkoCore.Commands.Abstractions;
using AkkoCore.Commands.Modules.Administration.Services;
using AkkoCore.Common;
using AkkoCore.Extensions;
using AkkoCore.Models.Serializable;
using AkkoCore.Models.Serializable.EmbedParts;
using AkkoCore.Services;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AkkoCore.Commands.Modules.Administration
{
    [Group("cooldown"), Aliases("cd")]
    [Description("cmd_cooldown")]
    public class Cooldown : AkkoCommandModule
    {
        private readonly CooldownService _service;

        public Cooldown(CooldownService service)
            => _service = service;

        [Command("add")]
        [Description("cmd_cooldown_add")]
        [RequireUserPermissions(Permissions.ManageGuild)]
        public async Task AddCmdCooldownAsync(CommandContext context, [Description("arg_cooldown_time")] TimeSpan time, [Description("arg_command")] string command)
        {
            if (command.StartsWith(context.Prefix))
                command = command[context.Prefix.Length..];

            var cmd = context.CommandsNext.FindCommand(command, out _);
            var success = (context.Guild is not null || GeneralService.IsOwner(context, context.User.Id))
                && await _service.AddCommandCooldownAsync(cmd, time, context.Guild);

            await context.Message.CreateReactionAsync((success) ? AkkoStatics.SuccessEmoji : AkkoStatics.FailureEmoji);
        }

        [Command("remove"), Aliases("rm")]
        [Description("cmd_cooldown_remove")]
        [RequireUserPermissions(Permissions.ManageGuild)]
        public async Task RemoveCmdCooldownAsync(CommandContext context, [Description("arg_command")] string command)
        {
            if (command.StartsWith(context.Prefix))
                command = command[context.Prefix.Length..];

            var success = (context.Guild is not null || GeneralService.IsOwner(context, context.User.Id))
                && await _service.RemoveCommandCooldownAsync(command, context.Guild?.Id);

            await context.Message.CreateReactionAsync((success) ? AkkoStatics.SuccessEmoji : AkkoStatics.FailureEmoji);
        }

        [GroupCommand, Command("list"), Aliases("show")]
        [Description("cmd_cooldown_list")]
        public async Task ListCmdCooldownsAsync(CommandContext context)
        {
            var commands = _service.GetCooldownCommands(context.Guild);
            var embed = new SerializableDiscordEmbed();

            if (!commands.Any())
            {
                embed.WithDescription("cooldown_empty");
                await context.RespondLocalizedAsync(embed, isError: true);

                return;
            }

            var fields = new List<SerializableEmbedField>();
            embed.WithTitle((context.Guild is null) ? "cooldown_title_global" : "cooldown_title_server");

            foreach (var commandGroup in commands.SplitInto(AkkoConstants.LinesPerPage))
            {
                fields.Add(new("command", string.Join("\n", commandGroup.Select(x => x.Key)), true));
                fields.Add(new("cooldown", string.Join("\n", commandGroup.Select(x => x.Value)), true));
            }

            await context.RespondPaginatedByFieldsAsync(embed, fields, 2);
        }
    }
}