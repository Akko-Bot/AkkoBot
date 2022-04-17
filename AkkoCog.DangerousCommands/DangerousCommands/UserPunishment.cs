using AkkoCore.Commands.Abstractions;
using AkkoCore.Commands.Modules.Administration.Services;
using AkkoCore.Common;
using AkkoCore.Extensions;
using AkkoCore.Models.Serializable;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System.Linq;
using System.Threading.Tasks;

namespace AkkoCog.DangerousCommands.Administration;  // Integrate with Administration

[RequireGuild]
public sealed class UserPunishment : AkkoCommandModule
{
    private readonly UserPunishmentService _service;

    public UserPunishment(UserPunishmentService punishService)
        => _service = punishService;

    [Command("massban")]
    [Description("cmd_massban")]
    [RequirePermissions(Permissions.BanMembers)]
    public async Task MassBanAsync(CommandContext context, [RemainingText, Description("arg_ulong_user_col")] params ulong[] userIds)
    {
        // Remove users that are already banned
        var massbanString = context.FormatLocalized("massban");
        var nonBanned = userIds
            .Except((await context.Guild.GetBansAsync()).Select(banned => banned.User.Id))
            .ToArray();

        // Send the confirmation message
        var embed = new SerializableDiscordEmbed()
            .WithTitle(":radioactive: " + massbanString)
            .WithDescription(context.FormatLocalized("massban_description", nonBanned.Length))
            .WithFooter(context.FormatLocalized("q_operation_length_seconds", nonBanned.Length * AkkoStatics.SafetyDelay.TotalSeconds));

        var result = await context.RespondLocalizedAsync(embed, false);

        // Trigger typing so the user knows the bot is doing something
        await context.TriggerTypingAsync();

        // Process the bans. Have in mind that IDs might be invalid
        var fails = 0;
        foreach (var userId in nonBanned)
        {
            try { await _service.BanUserAsync(context, userId, 1, context.Member!.GetFullname() + " | " + massbanString); }
            catch { fails += 1; }

            // Safety delay
            await Task.Delay(AkkoStatics.SafetyDelay);
        }

        // Check if no user got banned
        embed.WithDescription(
            (fails == nonBanned.Length)
                ? "massban_failure"
                : context.FormatLocalized("massban_success", nonBanned.Length - fails)
        );

        await result.ModifyLocalizedAsync(context, embed, false, fails == nonBanned.Length);
    }

    [Command("massunban")]
    [Description("cmd_massunban")]
    [RequirePermissions(Permissions.BanMembers)]
    public async Task MassUnbanAsync(CommandContext context, [RemainingText, Description("arg_ulong_user_col")] params ulong[] userIds)
    {
        var toUnban = (await context.Guild.GetBansAsync())
            .Select(banned => banned.User.Id)
            .Intersect(userIds)
            .ToArray();

        var failed = 0;
        if (toUnban.Length is not 0)
        {
            await context.TriggerTypingAsync();

            foreach (var userId in toUnban)
            {
                try { await context.Guild.UnbanMemberAsync(userId, $"{context.Member!.GetFullname()} | {context.FormatLocalized("massunban")}"); }
                catch { failed += 1; }

                await Task.Delay(AkkoStatics.SafetyDelay);
            }
        }

        var embed = new SerializableDiscordEmbed()
            .WithDescription(
                (failed == toUnban.Length)
                    ? "massunban_failed"
                    : context.FormatLocalized("massunban_succeded", toUnban.Length - failed)
            );

        await context.RespondLocalizedAsync(embed, true, failed == toUnban.Length);
    }
}