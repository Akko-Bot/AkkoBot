using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Converters;
using DSharpPlus.Entities;
using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AkkoCore.Commands.ArgumentConverters
{
    public class TimeSpanConverter : IArgumentConverter<TimeSpan>
    {
        private static readonly Regex _timeSpanRegex = new(
            @"^(\d+y)?(\d+mo)?(\d+w)?(\d+d)?(\d+h)?(\d+m)?(\d+s)?$",
            RegexOptions.Compiled
        );

        public Task<Optional<TimeSpan>> ConvertAsync(string input, CommandContext ctx)
        {
            // If input is just a number, return nothing
            if (int.TryParse(input, NumberStyles.Number, CultureInfo.InvariantCulture, out _))
                return Task.FromResult(Optional.FromNoValue<TimeSpan>());

            // If input is a TipeSpan string, return it - This conflics with TimeOfDay
            //if (TimeSpan.TryParse(input, CultureInfo.InvariantCulture, out var tsResult))
            //    return Task.FromResult(Optional.FromValue(tsResult));

            // Match with regex
            var match = _timeSpanRegex.Match(input.ToLowerInvariant());

            if (!match.Success)
                return Task.FromResult(Optional.FromNoValue<TimeSpan>());

            var result = TimeSpan.Zero;

            foreach (Group group in match.Groups.Values.Skip(1))
            {
                var groupValue = group.Value;

                // If group is not present in the input, skip
                if (string.IsNullOrWhiteSpace(groupValue))
                    continue;

                var type = (groupValue.EndsWith("mo")) ? "mo" : groupValue[(groupValue.Length - 1)..];
                int.TryParse(groupValue[..(groupValue.Length - 1)], NumberStyles.Integer, CultureInfo.InvariantCulture, out var number);

                var toAdd = type switch
                {
                    "s" => TimeSpan.FromSeconds(number),
                    "m" => TimeSpan.FromMinutes(number),
                    "h" => TimeSpan.FromHours(number),
                    "d" => TimeSpan.FromDays(number),
                    "w" => TimeSpan.FromDays(number * 7),
                    "mo" => TimeSpan.FromDays(number * 30),
                    "y" => TimeSpan.FromDays(number * 365),
                    _ => TimeSpan.Zero
                };

                result = result.Add(toAdd);
            }

            return Task.FromResult(Optional.FromValue(result));
        }
    }
}