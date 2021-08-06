using AkkoBot.Commands.Abstractions;
using AkkoBot.Commands.Attributes;
using AkkoBot.Common;
using AkkoBot.Extensions;
using AkkoBot.Models.Serializable;
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
    [RequireGuild]
    public class BasicGuildCommands : AkkoCommandModule
    {
        [Command("userid"), Aliases("uid")]
        [Description("cmd_userid")]
        public async Task UserIdAsync(CommandContext context, [Description("arg_discord_user")] DiscordMember user = null)
        {
            user ??= context.Member;

            var embed = new SerializableDiscordMessage()
                .WithDescription(context.FormatLocalized("xid", "user", Formatter.Bold(user.GetFullname()), Formatter.InlineCode(user.Id.ToString())));

            await context.RespondLocalizedAsync(embed);
        }

        [Command("channelid"), Aliases("cid")]
        [Description("cmd_channelid")]
        public async Task ChannelIdAsync(CommandContext context, [Description("arg_discord_channel")] DiscordChannel channel = null)
        {
            channel ??= context.Channel;

            var embed = new SerializableDiscordMessage()
                .WithDescription(context.FormatLocalized("xid", "channel", Formatter.Bold(channel.Mention), Formatter.InlineCode(channel.Id.ToString())));

            await context.RespondLocalizedAsync(embed);
        }

        [Command("serverid"), Aliases("sid")]
        [Description("cmd_serverid")]
        public async Task ServerIdAsync(CommandContext context)
        {
            var embed = new SerializableDiscordMessage()
                .WithDescription(context.FormatLocalized("xid", "server", Formatter.Bold(context.Guild.Name), Formatter.InlineCode(context.Guild.Id.ToString())));

            await context.RespondLocalizedAsync(embed);
        }

        [Command("roleid"), Aliases("rid")]
        [Description("cmd_roleid")]
        public async Task RoleIdAsync(CommandContext context, [Description("arg_discord_role")] DiscordRole role)
        {
            var embed = new SerializableDiscordMessage()
                .WithDescription(context.FormatLocalized("xid", "role", Formatter.Bold(role.Name), Formatter.InlineCode(role.Id.ToString())));

            await context.RespondLocalizedAsync(embed);
        }

        [Command("rolecolor"), Aliases("rcolor")]
        [Description("cmd_rolecolor")]
        public async Task RoleColorAsync(CommandContext context, [Description("arg_discord_role")] DiscordRole role, [Description("arg_discord_color")] DiscordColor? newColor = null)
        {
            if (newColor is null)
            {
                var embed = new SerializableDiscordMessage()
                    .WithDescription(role.Color.ToString());

                await context.RespondLocalizedAsync(embed, isMarked: false);
                return;
            }

            var success = role.Position < context.Guild.CurrentMember.Hierarchy
                && role.Position < context.Member.Hierarchy
                && context.Member.PermissionsIn(context.Channel).HasPermission(Permissions.ManageRoles)
                && context.Guild.CurrentMember.PermissionsIn(context.Channel).HasPermission(Permissions.ManageRoles);

            if (success)
                await role.ModifyAsync(x => x.Color = newColor);

            await context.Message.CreateReactionAsync((success) ? AkkoEntities.SuccessEmoji : AkkoEntities.FailureEmoji);
        }

        [Command("setnickname"), HiddenOverload]
        public async Task SetNicknameAsync(CommandContext context, string nickname = "")
            => await SetNicknameAsync(context, context.Guild.CurrentMember, nickname);

        [Command("setnickname"), Aliases("setnick")]
        [Description("cmd_setnickname")]
        [RequirePermissions(Permissions.ManageNicknames)]
        [RequireBotPermissions(Permissions.ChangeNickname)]
        public async Task SetNicknameAsync(
            CommandContext context,
            [Description("arg_discord_user")] DiscordMember user = null,
            [RemainingText, Description("arg_nickname")] string nickname = "")
        {
            var success = context.Guild.CurrentMember.Hierarchy >= user.Hierarchy && context.Member.Hierarchy > user.Hierarchy;

            if (success)
                await user.ModifyAsync(x => x.Nickname = nickname.MaxLength(AkkoConstants.MaxUsernameLength));

            await context.Message.CreateReactionAsync((success) ? AkkoEntities.SuccessEmoji : AkkoEntities.FailureEmoji);
        }

        [Command("listroles"), Aliases("roles")]
        [Description("cmd_listroles")]
        public async Task ListRolesAsync(CommandContext context, [Description("arg_discord_user")] DiscordMember user = null)
        {
            var roles = (user?.Roles ?? context.Guild.Roles.Values)
                .OrderByDescending(x => x.Position)
                .Select(x => $"• {x.Name}")
                .SplitInto(AkkoConstants.LinesPerPage);     // x roles per page

            var title = (user is null)
                ? "roles_title"
                : context.FormatLocalized("userroles_title", user.Username);

            var embed = new SerializableDiscordMessage();

            foreach (var roleGroup in roles)
                embed.AddField(title, string.Join('\n', roleGroup.ToArray()));

            await context.RespondPaginatedByFieldsAsync(embed, 1);
        }

        [Command("inrole")]
        [Description("cmd_inrole")]
        public async Task InRoleAsync(CommandContext context, [Description("arg_discord_role")] DiscordRole role)
        {
            var users = context.Guild.Members.Values.Where(x => x.Roles.Contains(role))
                .OrderByDescending(x => x.Hierarchy)
                .Select(x => $"● {x.GetFullname()}")
                .SplitInto(AkkoConstants.LinesPerPage);     // x users per page

            var title = context.FormatLocalized("inrole_title", role.Name);
            var embed = new SerializableDiscordMessage()
                .WithFooter(context.FormatLocalized("total_of", ((users.Count - 1) * AkkoConstants.LinesPerPage) + users.LastOrDefault().Count));

            foreach (var userGroup in users)
                embed.AddField(title, string.Join('\n', userGroup.ToArray()));

            await context.RespondPaginatedByFieldsAsync(embed, 1);
        }

        [RequireGuild]
        [RequirePermissions(Permissions.ManageChannels)]
        public class BasicChannelCommands : AkkoCommandModule
        {
            [Command("createtextchannel"), Aliases("ctch")]
            [Description("cmd_createtextchannel")]
            public async Task CreateTextChannelAsync(CommandContext context, [RemainingText, Description("arg_channel_name")] string name)
            {
                await context.Guild.CreateChannelAsync(name, ChannelType.Text, context.Message.Channel.Parent);
                await context.Message.CreateReactionAsync(AkkoEntities.SuccessEmoji);
            }

            [Command("createvoicechannel"), Aliases("cvch")]
            [Description("cmd_createvoicechannel")]
            public async Task CreateVoiceChannelAsync(CommandContext context, [RemainingText, Description("arg_channel_name")] string name)
            {
                await context.Guild.CreateChannelAsync(name, ChannelType.Voice, context.Message.Channel.Parent);
                await context.Message.CreateReactionAsync(AkkoEntities.SuccessEmoji);
            }

            [Command("createcategorychannel"), Aliases("ccch")]
            [Description("cmd_createcategorychannel")]
            public async Task CreateCategoryChannelAsync(CommandContext context, [RemainingText, Description("arg_channel_name")] string name)
            {
                await context.Guild.CreateChannelAsync(name, ChannelType.Category);
                await context.Message.CreateReactionAsync(AkkoEntities.SuccessEmoji);
            }

            [Command("deletechannel"), Aliases("dtch", "dvch", "dch")]
            [Description("cmd_deletechannel")]
            public async Task DeleteChannelAsync(CommandContext context, [Description("arg_discord_channel")] DiscordChannel channel)
            {
                await context.Message.CreateReactionAsync(AkkoEntities.SuccessEmoji);
                _ = channel.DeleteAsync();  // This may be flimsy
            }

            [Command("deletechannel"), HiddenOverload]
            public async Task DeleteChannelAsync(CommandContext context, [RemainingText] string name)
            {
                var channel = context.Guild.Channels.Values
                    .FirstOrDefault(x => x.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase) || x.Name.Equals(name.ToTextChannelName()));

                if (channel is not null)
                {
                    await context.Message.CreateReactionAsync(AkkoEntities.SuccessEmoji);
                    _ = channel.DeleteAsync();
                }
            }

            [Command("recreatechannel"), Aliases("rcc")]
            [Description("cmd_recreatechannel")]
            public async Task RecreateChannelAsync(CommandContext context, [Description("arg_discord_channel")] DiscordChannel channel)
            {
                var permissionOverwrites = new List<DiscordOverwriteBuilder>(channel.PermissionOverwrites.Count);

                foreach (var overwrite in channel.PermissionOverwrites)
                {
                    if (overwrite.Type is OverwriteType.Member)
                        permissionOverwrites.Add(await new DiscordOverwriteBuilder(await overwrite.GetMemberAsync()).FromAsync(overwrite));
                    else
                        permissionOverwrites.Add(await new DiscordOverwriteBuilder(await overwrite.GetRoleAsync()).FromAsync(overwrite));
                }


                var newChannel = await context.Guild.CreateChannelAsync(
                    channel.Name, channel.Type, channel.Parent, channel.Topic,
                    (channel.Type is ChannelType.Voice) ? channel.Bitrate : null,
                    channel.UserLimit, permissionOverwrites, channel.IsNSFW, channel.PerUserRateLimit
                );

                await newChannel.ModifyPositionAsync(channel.Position);

                await context.Message.CreateReactionAsync(AkkoEntities.SuccessEmoji);
                _ = channel.DeleteAsync();
            }

            [Command("settopic"), Aliases("st")]
            [Description("cmd_settopic")]
            public async Task SetTopicAsync(CommandContext context, [RemainingText, Description("arg_channel_topic")] string topic)
            {
                await context.Channel.ModifyAsync(x => x.Topic = topic);
                await context.Message.CreateReactionAsync(AkkoEntities.SuccessEmoji);
            }

            [Command("nsfwtoggle"), Aliases("nsfw")]
            [Description("cmd_nsfwtoggle")]
            public async Task ToggleNsfwAsync(CommandContext context)
            {
                await context.Channel.ModifyAsync(x => x.Nsfw = !context.Channel.IsNSFW);

                var embed = new SerializableDiscordMessage()
                    .WithDescription(context.FormatLocalized("nsfw_toggle", (context.Channel.IsNSFW) ? "enabled" : "disabled"));

                await context.RespondLocalizedAsync(embed);
            }

            [Command("slowmode")]
            [Description("cmd_slowmode")]
            public async Task SlowModeAsync(CommandContext context, TimeSpan? time = null)
            {
                if (time.HasValue && time.Value > TimeSpan.FromHours(6))
                    time = TimeSpan.FromHours(6);

                await context.Channel.ModifyAsync(x => x.PerUserRateLimit = (int)(time?.TotalSeconds ?? 0));
                await context.Message.CreateReactionAsync(AkkoEntities.SuccessEmoji);
            }

            [Command("renamechannel"), Aliases("rch")]
            [Description("cmd_renamechannel")]
            public async Task RenameChannelAsync(CommandContext context,
                [Description("arg_discord_channel")] DiscordChannel channel,
                [RemainingText, Description("arg_channel_newname")] string newName)
            {
                await channel.ModifyAsync(x => x.Name = newName);
                await context.Message.CreateReactionAsync(AkkoEntities.SuccessEmoji);
            }

            [Command("moveuser")]
            [Description("cmd_moveuser")]
            public async Task MoveUserAsync(CommandContext context, [Description("arg_discord_user")] DiscordMember user, [Description("arg_discord_voicechannel")] DiscordChannel channel = null)
            {
                var success = user.VoiceState is not null
                    && channel?.Type is null or ChannelType.Voice
                    && context.Member.Hierarchy > user.Hierarchy;

                if (success)
                    await user.ModifyAsync(x => x.VoiceChannel = channel);

                await context.Message.CreateReactionAsync((success) ? AkkoEntities.SuccessEmoji : AkkoEntities.FailureEmoji);
            }
        }

        [RequireGuild]
        [RequirePermissions(Permissions.ManageRoles)]
        public class BasicRoleCommands : AkkoCommandModule
        {
            #region Role Management

            [Command("createrole"), Aliases("cr")]
            [Description("cmd_createrole")]
            public async Task CreateRoleAsync(CommandContext context, [RemainingText, Description("arg_role_name")] string name = null)
            {
                await context.Guild.CreateRoleAsync(name);
                await context.Message.CreateReactionAsync(AkkoEntities.SuccessEmoji);
            }

            [Command("deleterole"), Aliases("dr")]
            [Description("cmd_deleterole")]
            public async Task DeleteRoleAsync(CommandContext context, [Description("arg_discord_role")] DiscordRole role)
            {
                var success = CheckRoleHierarchy(context.Guild.CurrentMember, context.Member, role);

                if (success)
                    await role.DeleteAsync();

                await context.Message.CreateReactionAsync((success) ? AkkoEntities.SuccessEmoji : AkkoEntities.FailureEmoji);
            }

            [Command("deleterole"), HiddenOverload]
            public async Task DeleteRoleAsync(CommandContext context, [RemainingText] string name)
            {
                var role = context.Guild.Roles.Values.FirstOrDefault(x => x.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
                var success = CheckRoleHierarchy(context.Guild.CurrentMember, context.Member, role);

                if (success)
                    await role.DeleteAsync();

                await context.Message.CreateReactionAsync((success) ? AkkoEntities.SuccessEmoji : AkkoEntities.FailureEmoji);
            }

            [Command("recreaterole"), Aliases("rcr")]
            [Description("cmd_recreaterole")]
            public async Task RecreateRoleAsync(CommandContext context, [Description("arg_discord_role")] DiscordRole role)
            {
                var success = CheckRoleHierarchy(context.Guild.CurrentMember, context.Member, role);

                if (success)
                {
                    var newRole = await context.Guild.CreateRoleAsync(role.Name, role.Permissions, role.Color, role.IsHoisted, role.IsMentionable);

                    await newRole.ModifyPositionAsync(role.Position);
                    await role.DeleteAsync();
                }

                await context.Message.CreateReactionAsync((success) ? AkkoEntities.SuccessEmoji : AkkoEntities.FailureEmoji);
            }

            [Command("renamerole"), Aliases("renr")]
            [Description("cmd_renamerole")]
            public async Task RenameRoleAsync(CommandContext context,
                [Description("arg_discord_role")] DiscordRole role,
                [RemainingText, Description("arg_role_newname")] string newName)
            {
                var success = CheckRoleHierarchy(context.Guild.CurrentMember, context.Member, role);

                if (success)
                    await role.ModifyAsync(x => x.Name = newName);

                await context.Message.CreateReactionAsync((success) ? AkkoEntities.SuccessEmoji : AkkoEntities.FailureEmoji);
            }

            [Command("rolehoist"), Aliases("hoistrole", "rh")]
            [Description("cmd_rolehoist")]
            public async Task RoleHoistAsync(CommandContext context, [Description("arg_discord_role")] DiscordRole role)
            {
                var success = CheckRoleHierarchy(context.Guild.CurrentMember, context.Member, role);

                if (success)
                    await role.ModifyAsync(x => x.Hoist = !role.IsHoisted);

                await context.Message.CreateReactionAsync((success) ? AkkoEntities.SuccessEmoji : AkkoEntities.FailureEmoji);
            }

            [Command("deleteallroles"), Aliases("dar")]
            [Description("cmd_deleteallroles")]
            public async Task RemoveAllRolesAsync(CommandContext context)
            {
                var roles = context.Guild.Roles.Values
                    .Where(role => CheckRoleHierarchy(context.Guild.CurrentMember, context.Member, role) && !role.Name.Equals("@everyone"))
                    .ToArray();

                if (roles.Length == 0)
                {
                    var embed = new SerializableDiscordMessage()
                        .WithDescription("deleteallroles_error");

                    await context.RespondLocalizedAsync(embed, isError: true);
                    return;
                }

                // Build the confirmation message
                var question = new SerializableDiscordMessage()
                    .WithFooter(context.FormatLocalized("q_operation_length_seconds", roles.Length * AkkoEntities.SafetyDelay.TotalSeconds))
                    .WithDescription(
                        context.FormatLocalized(
                            "q_are_you_sure",                                                           // Key
                            context.FormatLocalized("q_deleteallroles", roles.Length), "q_yes", "q_no"  // Values
                        )
                    );

                await context.RespondInteractiveAsync(question, "q_yes", async () =>
                {
                    await context.TriggerTypingAsync();

                    foreach (var role in roles)
                    {
                        await role.DeleteAsync();
                        await Task.Delay(AkkoEntities.SafetyDelay);
                    }

                    var embed = new SerializableDiscordMessage()
                        .WithDescription(context.FormatLocalized("deleteallroles", roles.Length));

                    await context.RespondLocalizedAsync(embed);
                });
            }

            #endregion Role Management

            #region Role Assignment

            [Command("setrole"), Aliases("sr")]
            [Description("cmd_setrole")]
            public async Task SetRoleAsync(CommandContext context,
                [Description("arg_discord_user")] DiscordMember user,
                [Description("arg_discord_role")] DiscordRole role)
            {
                var success = CheckRoleHierarchy(context.Guild.CurrentMember, context.Member, role);

                if (success)
                    await user.GrantRoleAsync(role);

                await context.Message.CreateReactionAsync((success) ? AkkoEntities.SuccessEmoji : AkkoEntities.FailureEmoji);
            }

            [Command("removerole"), Aliases("rr")]
            [Description("cmd_removerole")]
            public async Task RemoveRoleAsync(CommandContext context,
                [Description("arg_discord_user")] DiscordMember user,
                [Description("arg_discord_role")] DiscordRole role)
            {
                var success = CheckRoleHierarchy(context.Guild.CurrentMember, context.Member, role);

                if (success)
                    await user.RevokeRoleAsync(role);

                await context.Message.CreateReactionAsync((success) ? AkkoEntities.SuccessEmoji : AkkoEntities.FailureEmoji);
            }

            [Command("removeallroles"), Aliases("rar")]
            [Description("cmd_removeallroles")]
            public async Task RemoveAllRolesAsync(CommandContext context, [Description("arg_discord_user")] DiscordMember user)
            {
                var roles = user.Roles
                    .Where(role => CheckRoleHierarchy(context.Guild.CurrentMember, user, role))
                    .ToArray();

                await context.TriggerTypingAsync();

                foreach (var role in roles)
                {
                    await user.RevokeRoleAsync(role);
                    await Task.Delay(AkkoEntities.SafetyDelay);
                }

                // add thing for length 0
                var embed = new SerializableDiscordMessage()
                    .WithDescription(context.FormatLocalized("removeallroles", roles.Length, Formatter.Bold(user.GetFullname())));

                await context.RespondLocalizedAsync(embed);
            }

            #endregion Role Assignment

            /// <summary>
            /// Checks the hierarchical position between two Discord users and a role and determines whether it is safe to interact with the role or not.
            /// </summary>
            /// <param name="bot">The bot user.</param>
            /// <param name="user">The user who invoked the command.</param>
            /// <param name="role">The role to be checked.</param>
            /// <returns><see langword="true"/> if <paramref name="role"/> can be safely manipulated, <see langword="false"/> otherwise.</returns>
            private bool CheckRoleHierarchy(DiscordMember bot, DiscordMember user, DiscordRole role)
            {
                return bot is not null && user is not null && role is not null
                    && role.Position < bot.Hierarchy
                    && role.Position < user.Hierarchy
                    && !role.IsManaged;
            }
        }
    }
}