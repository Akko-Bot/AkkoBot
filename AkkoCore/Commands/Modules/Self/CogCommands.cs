using AkkoCore.Commands.Abstractions;
using AkkoCore.Common;
using AkkoCore.Core.Abstractions;
using AkkoCore.Extensions;
using AkkoCore.Models.Serializable;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Kotz.Extensions;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AkkoCore.Commands.Modules.Self;

[Group("cogs"), Aliases("cog")]
[Description("cmd_cogs")]
public sealed class CogCommands : AkkoCommandModule
{
    private readonly DiscordEmoji _cogEmoji = DiscordEmoji.FromUnicode("⚙️");
    private readonly ICogs _cogs;

    public CogCommands(ICogs cogs)
        => _cogs = cogs;

    [Command("info")]
    [Description("cmd_cogs_info")]
    public async Task CogInfoAsync(CommandContext context, [RemainingText, Description("arg_cog_name")] string name)
    {
        if (!_cogs.Headers.TryGetValue(name, out var cogHeader))
            cogHeader = _cogs.Headers.Values.FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        var embed = new SerializableDiscordEmbed();

        if (cogHeader is null)
            embed.WithDescription(context.FormatLocalized("cog_not_found", Formatter.InlineCode(name)));
        else
        {
            embed.WithAuthor($"{_cogEmoji} {context.FormatLocalized("cog_info_title")}")
                .WithTitle(cogHeader.Name)
                .WithDescription(cogHeader.Description)
                .AddField("version", cogHeader.Version, true)
                .AddField("author", cogHeader.Author, true);
        }

        await context.RespondLocalizedAsync(embed, cogHeader is null, cogHeader is null);
    }

    [Command("list"), GroupCommand]
    [Description("cmd_cogs_list")]
    public async Task ListCogsAsync(CommandContext context)
    {
        var embed = new SerializableDiscordEmbed();

        if (_cogs.Headers.Count is 0)
            embed.WithDescription("cogs_empty");
        else
        {
            embed.WithTitle("cogs_list_title");
            embed.AddField("name", string.Join('\n', _cogs.Headers.Keys), true);
            embed.AddField("description", string.Join('\n', _cogs.Headers.Values.Select(x => context.FormatLocalized(x.Description).MaxLength(47, AkkoConstants.EllipsisTerminator))), true);
        }

        await context.RespondLocalizedAsync(embed, isError: _cogs.Headers.Count is 0);
    }
}