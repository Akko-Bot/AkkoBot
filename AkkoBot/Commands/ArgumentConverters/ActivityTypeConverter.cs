using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Converters;
using DSharpPlus.Entities;
using System.Threading.Tasks;

namespace AkkoBot.Commands.ArgumentConverters
{
    public class ActivityTypeConverter : IArgumentConverter<ActivityType>
    {
        public Task<Optional<ActivityType>> ConvertAsync(string input, CommandContext ctx)
        {
            // Determine the appropriate type specified by the user
            // Bots are not allowed to have "Custom" activity, so it has been removed
            return input?.ToLowerInvariant() switch
            {
                "play" or "playing" => Task.FromResult(Optional.FromValue(ActivityType.Playing)),
                "stream" or "streaming" => Task.FromResult(Optional.FromValue(ActivityType.Streaming)),
                "listen" or "listening" => Task.FromResult(Optional.FromValue(ActivityType.ListeningTo)),
                "watch" or "watching" => Task.FromResult(Optional.FromValue(ActivityType.Watching)),
                "compete" or "competing" => Task.FromResult(Optional.FromValue(ActivityType.Competing)),
                _ => Task.FromResult(Optional.FromNoValue<ActivityType>())
            };
        }
    }
}