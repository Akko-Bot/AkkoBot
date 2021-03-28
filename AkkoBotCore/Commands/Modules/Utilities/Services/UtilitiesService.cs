using System.Net.Mail;
using System.Linq;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using AkkoBot.Commands.Abstractions;
using AkkoBot.Extensions;
using AkkoBot.Models;
using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;
using AkkoBot.Common;
using System;
using DSharpPlus.CommandsNext;
using System.Collections.Generic;

namespace AkkoBot.Commands.Modules.Utilities.Services
{
    /// <summary>
    /// Groups utility methods for the Utilities command module.
    /// </summary>
    public class UtilitiesService : ICommandService
    {
        private readonly IServiceProvider _services;

        public UtilitiesService(IServiceProvider services)
            => _services = services;

        /// <summary>
        /// Deserializes user input in Yaml to a Discord message.
        /// </summary>
        /// <param name="input">The user's input.</param>
        /// <param name="result">The deserialized input, <see langword="null"/> if deserialization fails.</param>
        /// <returns><see langword="true"/> if deserialization was successful, <see langword="false"/> otherwise.</returns>
        public bool DeserializeEmbed(string input, out DiscordMessageBuilder result)
        {
            try
            {
                result = input.FromYaml<SerializableEmbed>().BuildMessage();
                return result.Content is not null || result.Embed is not null;
            }
            catch
            {
                result = null;
                return false;
            }
        }

        /// <summary>
        /// Creates a GET request to the specified URL and returns the result as a stream.
        /// </summary>
        /// <param name="url">The URL to make the GET request.</param>
        /// <returns>A <see cref="Stream"/> of the requested URL.</returns>
        public async Task<Stream> GetOnlineStreamAsync(string url)
        {
            var http = _services.GetService<HttpClient>();

            // Stream needs to be seekable
            try { return await (await http.GetAsync(url)).Content.ReadAsStreamAsync(); }
            catch { return null; }
        }

        /// <summary>
        /// Adds an emoji to the context guild.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="emoji">The emoji to be added.</param>
        /// <param name="name">The name of the emoji.</param>
        /// <remarks>If an emoji with the same <paramref name="name"/> is found, it will be replaced with the new emoji.</remarks>
        /// <returns><see langword="true"/> if the emoji got added, <see langword="false"/> otherwise.</returns>
        public async Task<bool> AddGuildEmojiAsync(CommandContext context, DiscordEmoji emoji, string name = null)
        {
            name = name?.Trim(':') ?? emoji.Name.SanitizeEmojiName();
            var imageStream = await GetOnlineStreamAsync(emoji.Url);

            return await AddEmojiAsync(context, imageStream, name);
        }

        /// <summary>
        /// Adds an emoji to the context guild.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="url">Direct link to the image to be added as an emoji.</param>
        /// <param name="name">The name of the emoji. Defaults to the file name in the URL.</param>
        /// <remarks>If an emoji with the same <paramref name="name"/> is found, it will be replaced with the new emoji.</remarks>
        /// <returns><see langword="true"/> if the emoji got added, <see langword="false"/> otherwise.</returns>
        public async Task<bool> AddGuildEmojiAsync(CommandContext context, Uri url, string name = null)
        {
            // Check if file extension is supported
            if (!url.AbsoluteUri.Contains('.') || !url.AbsoluteUri[url.AbsoluteUri.LastIndexOf('.')..].StartsWith(AkkoEntities.SupportedEmojiFormats, StringComparison.InvariantCultureIgnoreCase))
                return false;

            var urlLeftPart = url.AbsoluteUri.RemoveExtension();
            var imageStream = await GetOnlineStreamAsync(url.AbsoluteUri);
            name = name?.Trim(':') ?? urlLeftPart[urlLeftPart.LastIndexOf('/')..].SanitizeEmojiName();

            return imageStream is not null && await AddEmojiAsync(context, imageStream, name);
        }

        /// <summary>
        /// Adds multiple emojis to the context guild.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="attachments">The attachments in a <see cref="DiscordMessage"/>.</param>
        /// <param name="additionalDelay">Time, in seconds, to add to the delay between processing each attachment.</param>
        /// <returns><see langword="true"/> if at least one emoji got added, <see langword="false"/> otherwise.</returns>
        public async Task<bool> AddGuildEmojisAsync(CommandContext context, IEnumerable<DiscordAttachment> attachments, double additionalDelay = 0)
        {
            var success = false;
            await context.TriggerTypingAsync();

            foreach (var attachment in attachments)
            {
                // Check if file extension is supported
                if (!attachment.FileName.Contains('.') || !attachment.FileName[attachment.FileName.LastIndexOf('.')..].StartsWith(AkkoEntities.SupportedEmojiFormats, StringComparison.InvariantCultureIgnoreCase))
                    continue;

                var name = attachment.FileName.RemoveExtension();
                var imageStream = await GetOnlineStreamAsync(attachment.Url);
                var added = await AddEmojiAsync(context, imageStream, name);

                success = success || added;

                if (added)
                    await Task.Delay(AkkoEntities.SafetyDelay.Add(TimeSpan.FromSeconds(additionalDelay)));
            }

            return success;
        }

        /// <summary>
        /// Adds an emoji to the context guild.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="imageStream">The image stream to be added as an emoji.</param>
        /// <param name="name">The name of the emoji.</param>
        /// <returns><see langword="true"/> if the emoji got added, <see langword="false"/> otherwise.</returns>
        private async Task<bool> AddEmojiAsync(CommandContext context, Stream imageStream, string name)
        {
            // Emojis must have less than 256Kb
            var isStreamValid = imageStream is not null && imageStream.Length < 256000;

            if (isStreamValid)
            {
                try { await context.Guild.CreateEmojiAsync(name.SanitizeEmojiName(), imageStream, context.Message.MentionedRoles); }
                catch { isStreamValid = false; }
            }

            if (imageStream is not null)
                await imageStream.DisposeAsync();

            return isStreamValid;
        }
    }
}