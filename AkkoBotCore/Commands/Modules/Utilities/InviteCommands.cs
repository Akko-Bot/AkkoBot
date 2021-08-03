using AkkoBot.Commands.Abstractions;
using AkkoBot.Commands.Attributes;
using AkkoBot.Common;
using AkkoBot.Extensions;
using AkkoBot.Models.Serializable;
using AkkoBot.Models.Serializable.EmbedParts;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AkkoBot.Commands.Modules.Utilities
{
    [Group("invite"), Aliases("invites")]
    [Description("cmd_invite")]
    [RequireGuild]
    public class InviteCommands : AkkoCommandModule
    {
        [Command("add"), HiddenOverload]
        public async Task CreateInviteAsync(CommandContext context, TimeSpan expiresIn)
            => await CreateInviteAsync(context, default, expiresIn, default, default);

        [Command("add"), HiddenOverload]
        public async Task CreateInviteAsync(CommandContext context, int maxUses)
            => await CreateInviteAsync(context, default, default, maxUses, default);

        [Command("add"), HiddenOverload]
        public async Task CreateInviteAsync(CommandContext context, bool temporary)
            => await CreateInviteAsync(context, default, default, default, temporary);

        [Command("add"), HiddenOverload]
        public async Task CreateInviteAsync(CommandContext context, DiscordChannel channel, int maxUses, bool temporary = false)
            => await CreateInviteAsync(context, channel, default, maxUses, temporary);

        [Command("add"), HiddenOverload]
        public async Task CreateInviteAsync(CommandContext context, DiscordChannel channel, bool temporary = false)
            => await CreateInviteAsync(context, channel, default, default, temporary);

        [Command("add"), HiddenOverload]
        public async Task CreateInviteAsync(CommandContext context, TimeSpan expiresIn, int maxUses, bool temporary = false)
            => await CreateInviteAsync(context, default, expiresIn, maxUses, temporary);

        [Command("add"), HiddenOverload]
        public async Task CreateInviteAsync(CommandContext context, int maxUses, bool temporary = false)
            => await CreateInviteAsync(context, default, default, maxUses, temporary);

        [Command("add"), HiddenOverload]
        public async Task CreateInviteAsync(CommandContext context, TimeSpan expiresIn, bool temporary = false)
            => await CreateInviteAsync(context, default, expiresIn, default, temporary);

        [Command("add"), Aliases("create")]
        [Description("cmd_invite_add")]
        [RequirePermissions(Permissions.CreateInstantInvite)]
        public async Task CreateInviteAsync(
            CommandContext context,
            [Description("arg_discord_channel")] DiscordChannel channel = null,
            [Description("arg_invite_time")] TimeSpan expiresIn = default,
            [Description("arg_invite_uses")] int maxUses = default,
            [Description("arg_invite_temporary")] bool temporary = false)
        {
            if (expiresIn > TimeSpan.FromDays(7))
            {
                await context.Message.CreateReactionAsync(AkkoEntities.FailureEmoji);
                return;
            }

            channel ??= context.Guild.GetDefaultChannel();
            var invite = await channel.CreateInviteAsync((int)expiresIn.TotalSeconds, maxUses, temporary);

            await context.Channel.SendMessageAsync(invite.ToString());
        }

        [Command("remove"), Aliases("rm")]
        [Description("cmd_invite_remove")]
        [RequirePermissions(Permissions.CreateInstantInvite)]
        public async Task RemoveInviteAsync(CommandContext context, [Description("arg_discord_invite")] string invite)
        {
            var guildInvite = (await context.Guild.GetInvitesAsync()).FirstOrDefault(x => invite.Contains(x.Code, StringComparison.Ordinal));

            if (guildInvite is not null)
                await guildInvite.DeleteAsync();

            await context.Message.CreateReactionAsync((guildInvite is not null) ? AkkoEntities.SuccessEmoji : AkkoEntities.FailureEmoji);
        }

        [GroupCommand, Command("list"), Aliases("show")]
        [Description("cmd_invite_list")]
        public async Task ListInvitesAsync(CommandContext context)
        {
            var fields = new List<SerializableEmbedField>();
            var invites = (await context.Guild.GetInvitesAsync())
                .OrderByDescending(x => x.Uses)
                .SplitInto(AkkoConstants.LinesPerPage);

            foreach (var group in invites)
            {
                fields.Add(new("code", string.Join("\n", group.Select(x => x.Code)), true));
                fields.Add(new("used", string.Join("\n", group.Select(x => context.FormatLocalized("x_times", x.Uses))), true));
                fields.Add(new("expires_at", string.Join("\n", group.Select(x => (x.MaxAge is 0) ? "-" : x.CreatedAt.AddSeconds(x.MaxAge).ToDiscordTimestamp())), true));
            }

            var embed = new SerializableDiscordMessage()
                .WithTitle("invite_list_title");

            await context.RespondPaginatedByFieldsAsync(embed, fields, 3);
        }

        [Command("check")]
        [Description("cmd_invite_check")]
        public async Task CheckInviteAsync(CommandContext context, [Description("arg_discord_invite")] string invite)
        {
            var embed = new SerializableDiscordMessage();
            var guildInvite = (await context.Guild.GetInvitesAsync()).FirstOrDefault(x => invite.Contains(x.Code, StringComparison.Ordinal));

            if (guildInvite is null)
                embed.WithDescription("invite_not_found");
            else
            {
                embed.WithTitle("invite_check_title")
                    .WithDescription(guildInvite.GetInviteLink())
                    .AddField("author", guildInvite.Inviter.GetFullname(), true)
                    .AddField("code", guildInvite.Code, true)
                    .AddField("created_at", guildInvite.CreatedAt.ToDiscordTimestamp(), true)
                    .AddField("channel", $"<#{guildInvite.Channel.Id}>", true)
                    .AddField("invite_temporary", (guildInvite.IsTemporary) ? AkkoEntities.SuccessEmoji.Name : AkkoEntities.FailureEmoji.Name, true)
                    .AddField("expires_at", (guildInvite.MaxAge is 0) ? "-" : guildInvite.CreatedAt.AddSeconds(guildInvite.MaxAge).ToDiscordTimestamp(), true)
                    .AddField("used", context.FormatLocalized("x_times", guildInvite.Uses), true)
                    .AddField("uses_left", (guildInvite.MaxUses is 0) ? "-" : (guildInvite.MaxUses - guildInvite.Uses).ToString(), true)
                    .AddField("expires_in", (guildInvite.MaxAge is 0) ? "-" : guildInvite.CreatedAt.AddSeconds(guildInvite.MaxAge).ToDiscordTimestamp(DiscordTimestamp.RelativeTime), true);
            }

            await context.RespondLocalizedAsync(embed, guildInvite is null, guildInvite is null);
        }
    }
}