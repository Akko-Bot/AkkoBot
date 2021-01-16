using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Threading.Tasks;
using AkkoBot.Services.Database.Abstractions;
using AkkoBot.Services.Database.Entities;
using AkkoBot.Services.Localization.Abstractions;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace AkkoBot.Extensions
{
    public static class CommandContextExt
    {
        // public static async Task ReplyLocalizedAsync(this CommandContext context, string response)
        // {
        //     var (localizer, guild) = await GetServices(context);
        //     var responseString = localizer.GetResponseString(guild.Locale, response);

        //     await context.RespondAsync(responseString);
        // }

        // public static async Task ReplyLocalizedAsync(this CommandContext context, string format, params string[] responses)
        // {
        //     var (localizer, guild) = await GetServices(context);
        //     var responseStrings = localizer.GetResponseStrings(guild.Locale, responses);

        //     await context.RespondAsync(string.Format(format, responseStrings));
        // }

        public static async Task ReplyLocalizedAsync(this CommandContext context, DiscordEmbedBuilder embed, bool isMarked = true, bool isError = false)
            => await ReplyLocalizedAsync(context, null, embed, isMarked, isError);

        public static async Task ReplyLocalizedAsync(this CommandContext context, string message, DiscordEmbedBuilder embed, bool isMarked = true, bool isError = false)
        {
            using var scope = context.CommandsNext.Services.CreateScope();
            var (localizer, guild) = await GetServices(scope, context);
            var responseString = localizer.GetResponseString(guild.Locale, message);
            var localizedEmbed = LocalizeEmbed(localizer, guild, embed, isError);


            if (isMarked)
                localizedEmbed.Description = localizedEmbed.Description.Insert(0, Formatter.Bold($"{context.User.Username}#{context.User.Discriminator} "));

            if (guild.UseEmbed)
                await context.RespondAsync(responseString, false, localizedEmbed);
            else
                await context.RespondAsync(responseString + "\n\n" + DeconstructEmbed(embed));
        }

        private static async Task<(ILocalizer, GuildConfigEntity)> GetServices(IServiceScope scope, CommandContext context)
        {
            // TODO: Use create scope for IUnitOfWork
            var guild = await scope.ServiceProvider.GetService<IUnitOfWork>().GuildConfigs.GetGuildAsync(context.Guild.Id);
            var localizer = scope.ServiceProvider.GetService<ILocalizer>();

            return (localizer, guild);
        }

        private static DiscordEmbedBuilder LocalizeEmbed(ILocalizer localizer, GuildConfigEntity guild, DiscordEmbedBuilder embed, bool isError = false)
        {
            if (embed.Title is not null)
                embed.Title = SetToResponseString(localizer, guild.Locale, embed.Title);

            if (embed.Description is not null)
                embed.Description = SetToResponseString(localizer, guild.Locale, embed.Description);

            if (embed.Url is not null)
                embed.Url = SetToResponseString(localizer, guild.Locale, embed.Url);

            if (!embed.Color.HasValue)
                embed.Color = new DiscordColor((isError) ? guild.ErrorColor : guild.OkColor);

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

        private static string DeconstructEmbed(this DiscordEmbedBuilder embed)
        {
            var dEmbed = new StringBuilder(
                ((embed.Author is null) ? string.Empty : embed.Author.Name + "\n\n") +
                ((embed.Title is null) ? string.Empty : embed.Title + "\n") +
                ((embed.Description is null) ? string.Empty : embed.Description + "\n\n")
            );

            foreach (var field in embed.Fields)
                dEmbed.AppendLine(field.Name + "\n" + field.Value + "\n");

            dEmbed.Append(
                ((embed.ImageUrl is null) ? string.Empty : $"{embed.ImageUrl}\n\n") +
                ((embed.Footer is null) ? string.Empty : embed.Footer.Text + "\n") +
                ((embed.Timestamp is null) ? string.Empty : embed.Timestamp.ToString())
            );

            return dEmbed.ToString();
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