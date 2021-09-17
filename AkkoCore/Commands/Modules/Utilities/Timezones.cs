using AkkoCore.Commands.Abstractions;
using AkkoCore.Common;
using AkkoCore.Extensions;
using AkkoCore.Models.Serializable;
using AkkoCore.Models.Serializable.EmbedParts;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AkkoCore.Commands.Modules.Utilities
{
    public class Timezones : AkkoCommandModule
    {
        [Command("timezone")]
        [Description("cmd_timezone")]
        public async Task ServerTimezoneAsync(CommandContext context)
        {
            var timezone = context.GetTimeZone();
            var embed = new SerializableDiscordEmbed()
                .WithDescription(context.FormatLocalized("server_timezone", Formatter.InlineCode($"{timezone.StandardName} ({timezone.BaseUtcOffset.Hours:00}:{timezone.BaseUtcOffset.Minutes:00})")));

            await context.RespondLocalizedAsync(embed);
        }

        [Command("timezones")]
        [Description("cmd_timezones")]
        public async Task TimezoneAsync(CommandContext context)
        {
            var fields = new List<SerializableEmbedField>();
            var timezones = TimeZoneInfo.GetSystemTimeZones()
                .OrderBy(x => x.Id)
                .Select(x => x.Id)
                .SplitInto(AkkoConstants.LinesPerPage);

            var embed = new SerializableDiscordEmbed()
                .WithTitle("timezone_list");

            foreach (var group in timezones)
                fields.Add(new(AkkoConstants.ValidWhitespace, string.Join("\n", group), true));

            await context.RespondPaginatedByFieldsAsync(embed, fields, 2);
        }
    }
}