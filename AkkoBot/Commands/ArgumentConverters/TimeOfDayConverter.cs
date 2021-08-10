using AkkoBot.Extensions;
using AkkoEntities.Common;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Converters;
using DSharpPlus.Entities;
using System;
using System.Threading.Tasks;

namespace AkkoBot.Commands.ArgumentConverters
{
    public class TimeOfDayConverter : IArgumentConverter<TimeOfDay>
    {
        public Task<Optional<TimeOfDay>> ConvertAsync(string input, CommandContext ctx)
        {
            var hourMinute = input.Split(':');

            if (hourMinute.Length != 2
                || !int.TryParse(hourMinute[0], out var hours) || !int.TryParse(hourMinute[1], out var minutes)
                || hours is < 0 or > 23 || minutes is < 0 or > 59)
                return Task.FromResult(Optional.FromNoValue<TimeOfDay>());

            var time = TimeSpan.FromHours(hours).Add(TimeSpan.FromMinutes(minutes));
            var timezone = ctx.GetTimeZone();

            return Task.FromResult(Optional.FromValue(new TimeOfDay(time, timezone)));
        }
    }
}