using AkkoBot.Commands.Abstractions;
using AkkoBot.Extensions;
using AkkoBot.Models.Serializable;
using AkkoBot.Models.Serializable.EmbedParts;
using AkkoCore.Common;
using AkkoCore.Extensions;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
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
        public async Task ServerTimezoneAsync(CommandContext context)
        {
            var timezone = context.GetTimeZone();
            var embed = new SerializableDiscordMessage()
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

            var embed = new SerializableDiscordMessage()
                .WithTitle("timezone_list");

            foreach (var group in timezones)
                fields.Add(new(AkkoConstants.ValidWhitespace, string.Join("\n", group), true));

            await context.RespondPaginatedByFieldsAsync(embed, fields, 2);
        }
    }
}