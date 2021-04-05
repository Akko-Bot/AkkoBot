using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Converters;
using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Threading.Tasks;

namespace AkkoBot.Commands.ArgumentConverters
{
    public class DiscordGuildConverter : IArgumentConverter<DiscordGuild>
    {
        public Task<Optional<DiscordGuild>> ConvertAsync(string input, CommandContext ctx)
        {
            if (!ulong.TryParse(input, out var sid))
                return Task.FromResult(Optional.FromNoValue<DiscordGuild>());

            var clients = ctx.Services.GetService<DiscordShardedClient>();
            var server = clients.ShardClients.Values.SelectMany(client => client.Guilds.Values).FirstOrDefault(server => server.Id == sid);

            return (server is null)
                ? Task.FromResult(Optional.FromNoValue<DiscordGuild>())
                : Task.FromResult(Optional.FromValue(server));
        }
    }
}