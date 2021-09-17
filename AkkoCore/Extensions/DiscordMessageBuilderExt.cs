using AkkoCore.Models.Serializable;
using DSharpPlus.Entities;
using System.Linq;

namespace AkkoCore.Extensions
{
    public static class DiscordMessageBuilderExt
    {
        public static SerializableDiscordMessage ToSerializableMessage(this DiscordMessageBuilder messageBuilder)
        {
            return new SerializableDiscordMessage() { Content = messageBuilder.Content }
                .AddEmbeds(messageBuilder.Embeds.Select(x => x.ToSerializableEmbed()));
        }
    }
}
