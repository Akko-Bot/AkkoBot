using AkkoBot.Commands.Abstractions;
using AkkoBot.Common;
using AkkoBot.Extensions;
using AkkoBot.Models;
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
    public class Timezones : AkkoCommandModule
    {
        [Command("timezone")]
        [Description("cmd_timezone")]
        public async Task ServerTimezone(CommandContext context)
        {
            var timezone = context.GetTimeZone();
            var embed = new DiscordEmbedBuilder()
                .WithDescription(context.FormatLocalized("server_timezone", Formatter.InlineCode($"{timezone.StandardName} ({timezone.BaseUtcOffset.Hours:00}:{timezone.BaseUtcOffset.Minutes:00})")));

            await context.RespondLocalizedAsync(embed);
        }

        [Command("timezones")]
        [Description("cmd_timezones")]
        public async Task Timezone(CommandContext context)
        {
            var fields = new List<SerializableEmbedField>();
            var timezones = TimeZoneInfo.GetSystemTimeZones()
                .OrderBy(x => x.Id)
                .Select(x => x.Id)
                .SplitInto(AkkoConstants.LinesPerPage);

            var embed = new DiscordEmbedBuilder()
                .WithTitle("timezone_list");

            foreach (var group in timezones)
                fields.Add(new(AkkoConstants.ValidWhitespace, string.Join("\n", group), true));

            await context.RespondPaginatedByFieldsAsync(embed, fields, 2);
        }
    }
}