using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using AkkoBot.Services.Database.Abstractions;
using AkkoBot.Services.Database.Entities;
using AkkoBot.Services.Localization.Abstractions;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace AkkoBot.Extensions
{
    public static class CommandContextExt
    {
        public static async Task ReplyLocalizedAsync(this CommandContext context, string response)
        {
            var (localizer, guild) = await GetServices(context);
            var responseString = localizer.GetResponseString(guild.Locale, response);

            await context.RespondAsync(responseString);
        }

        public static async Task ReplyLocalizedAsync(this CommandContext context, string format, params string[] responses)
        {
            var (localizer, guild) = await GetServices(context);
            var responseStrings = localizer.GetResponseStrings(guild.Locale, responses);

            await context.RespondAsync(string.Format(format, responseStrings));
        }

        public static async Task ReplyLocalizedEmbedAsync(this CommandContext context, DiscordEmbedBuilder embed)
            => await ReplyLocalizedEmbedAsync(context, null, embed);

        public static async Task ReplyLocalizedEmbedAsync(this CommandContext context, string message, DiscordEmbedBuilder embed)
        {
            var (localizer, guild) = await GetServices(context);
            var responseString = localizer.GetResponseString(guild.Locale, message);
            var localizedEmbed = LocalizeEmbed(localizer, guild, embed);

            await context.RespondAsync(responseString, false, localizedEmbed);
        }

        private static async Task<(ILocalizer, GuildConfigEntity)> GetServices(CommandContext context)
        {
            var guild = await context.Services.GetService<IUnitOfWork>().GuildConfigs.GetGuildAsync(context.Guild.Id);
            var localizer = context.Services.GetService<ILocalizer>();

            return (localizer, guild);
        }

        private static DiscordEmbedBuilder LocalizeEmbed(ILocalizer localizer, GuildConfigEntity guild, DiscordEmbedBuilder embed)
        {
            if (embed.Title is not null)
                embed.Title = SetToResponseString(localizer, guild.Locale, embed.Title);

            if (embed.Description is not null)
                embed.Description = SetToResponseString(localizer, guild.Locale, embed.Description);

            if (embed.Url is not null)
                embed.Url = SetToResponseString(localizer, guild.Locale, embed.Url);

            if (!embed.Color.HasValue)
                embed.Color = new DiscordColor(guild.OkColor);

            if (embed.Author is not null)
                embed.Author.Name = SetToResponseString(localizer, guild.Locale, embed.Author.Name);

            if (embed.Footer is not null)
                embed.Footer.Text = SetToResponseString(localizer, guild.Locale, embed.Footer.Text);

            foreach (var field in embed.Fields)
            {
                field.Name = SetToResponseString(localizer, guild.Locale, field.Name);
                field.Value = SetToResponseString(localizer, guild.Locale, field.Value);
            }

            return embed;
        }

        private static string SetToResponseString(ILocalizer localizer, string locale, string sample)
        {
            if (localizer.ContainsResponse(sample))
                return localizer.GetResponseString(locale, sample);
            else
                return sample;
        }
    }
}