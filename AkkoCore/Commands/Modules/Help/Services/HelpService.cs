using AkkoCore.Commands.Attributes;
using AkkoCore.Common;
using AkkoCore.Config.Abstractions;
using AkkoCore.Config.Models;
using AkkoCore.Extensions;
using AkkoCore.Models.Serializable;
using AkkoCore.Services.Caching.Abstractions;
using AkkoCore.Services.Events.Abstractions;
using AkkoCore.Services.Localization.Abstractions;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AkkoCore.Commands.Modules.Help.Services;

/// <summary>
/// Contains helper methods for the Help module.
/// </summary>
[CommandService(ServiceLifetime.Singleton)]
public sealed class HelpService
{
    private readonly ILocalizer _localizer;
    private readonly IDbCache _dbCache;
    private readonly ICommandHandler _commandHandler;
    private readonly BotConfig _botConfig;

    public HelpService(ILocalizer localizer, IDbCache dbCache, ICommandHandler commandHandler, BotConfig botConfig)
    {
        _localizer = localizer;
        _dbCache = dbCache;
        _commandHandler = commandHandler;
        _botConfig = botConfig;
    }

    /// <summary>
    /// Gets all modules with registered commands.
    /// </summary>
    /// <param name="settings">The message settings of the current context.</param>
    /// <param name="cmdHandler">The command handler.</param>
    /// <param name="thisCmdQualifiedName">The qualified name of the command invoking this method.</param>
    /// <returns>A Discord embed with all registered modules.</returns>
    public SerializableDiscordEmbed GetAllModules(IMessageSettings settings, CommandsNextExtension cmdHandler, string thisCmdQualifiedName)
    {
        var namespaces = cmdHandler.RegisteredCommands.Values
            .Where(cmd => !cmd.IsHidden && !cmd.Module.ModuleType.FullName!.Contains("DSharpPlus"))                                             // Remove library modules
            .Select(cmd => cmd.Module.ModuleType.Namespace?[(cmd.Module.ModuleType.Namespace.LastOccurrenceOf('.', 0) + 1)..] ?? "Undefined")   // Get the module name
            .Distinct()                                                                                                                         // Remove the repeated modules
            .OrderBy(x => x);

        return new SerializableDiscordEmbed()
            .WithTitle("modules_title")
            .WithDescription(string.Join("\n", namespaces))
            .WithFooter(
                _localizer.FormatLocalized(
                    settings.Locale,
                    "modules_footer",
                    settings.Prefix + thisCmdQualifiedName +
                    " <" + _localizer.FormatLocalized(settings.Locale, "name").ToLowerInvariant() + ">"
                )
            );
    }

    /// <summary>
    /// Gets all commands under the specified module.
    /// </summary>
    /// <param name="settings">The message settings of the current context.</param>
    /// <param name="cmdHandler">The command handler.</param>
    /// <param name="user">The user who executed the command.</param>
    /// <param name="channel">The channel where the command was executed.</param>
    /// <param name="moduleName">The full or partial name of the module.</param>
    /// <remarks>Searches with no result return an embed with its color set to <see cref="IMessageSettings.ErrorColor"/>.</remarks>
    /// <returns>An embed with the commands from the specified module.</returns>
    public async Task<SerializableDiscordEmbed> GetAllModuleCommandsAsync(IMessageSettings settings, CommandsNextExtension cmdHandler, DiscordUser user, DiscordChannel channel, string moduleName)
    {
        var cmdGroup = await cmdHandler.RegisteredCommands.Values
            .Where(cmd => !cmd.IsHidden && cmd.Module.ModuleType.Namespace?.Contains(moduleName, StringComparison.InvariantCultureIgnoreCase) is true)
            .Distinct()
            .OrderBy(x => x.Name)
            .Select(async cmd =>
            {
                var fakeContext = cmdHandler.CreateFakeContext(user, channel, string.Empty, string.Empty, cmd);

                var emote = ((_commandHandler.GetActiveOverride(channel.GuildId, cmd, out var permOverride) && _commandHandler.IsAllowedOverridenContext(fakeContext, permOverride!)))
                    ? AkkoStatics.SuccessEmoji
                    : (permOverride is null || !permOverride.IsActive)
                        ? (await cmd.RunChecksAsync(fakeContext, false)).Any() ? AkkoStatics.FailureEmoji : AkkoStatics.SuccessEmoji
                        : AkkoStatics.FailureEmoji;

                return @"\" + emote + settings.Prefix + cmd.QualifiedName;
            })
            .WhenAllAsync();

        var embed = new SerializableDiscordEmbed();

        if (cmdGroup.Length is 0)
        {
            embed.WithDescription(_localizer.FormatLocalized(settings.Locale, "module_not_exist", Formatter.InlineCode(settings.Prefix + "modules")))
                .WithColor(settings.ErrorColor);
        }
        else
        {
            embed.WithFooter(
                    _localizer.FormatLocalized(
                        settings.Locale,
                        "command_modules_footer",
                        settings.Prefix + "help" +
                        " <" + _localizer.FormatLocalized(settings.Locale, "command").ToLowerInvariant() + ">"
                    )
                );

            // Split the results into 3 columns
            foreach (var column in cmdGroup.Chunk((int)Math.Ceiling(cmdGroup.Length / 3.0)))
                embed.AddField(AkkoConstants.ValidWhitespace, string.Join('\n', column), true);
        }

        return embed;
    }

    /// <summary>
    /// Searches for commands with the specified keyword.
    /// </summary>
    /// <param name="settings">The message settings of the current context.</param>
    /// <param name="cmdHandler">The command handler.</param>
    /// <param name="searchParameter">The parameter to search for.</param>
    /// <remarks>
    /// <br>The embed may contain more than 25 fields, so use it with pagination.</br>
    /// <br>Searches with no result return an embed with its color set to <see cref="IMessageSettings.ErrorColor"/>.</br>
    /// </remarks>
    /// <returns>A Discord embed with the search results.</returns>
    public SerializableDiscordEmbed SearchCommandByKeyword(IMessageSettings settings, CommandsNextExtension cmdHandler, string searchParameter)
    {
        if (searchParameter.StartsWith(settings.Prefix))
            searchParameter = searchParameter[settings.Prefix.Length..];

        var embed = new SerializableDiscordEmbed();
        var cmds = cmdHandler.GetAllCommands()
            .Where(x => x.QualifiedName.Contains(searchParameter, StringComparison.OrdinalIgnoreCase))
            .DistinctBy(x => x.QualifiedName)
            .OrderBy(x => x.QualifiedName);

        if (!cmds.Any())
        {
            return embed.WithDescription(_localizer.FormatLocalized(settings.Locale, "search_result_empty", Formatter.InlineCode(searchParameter)))
                .WithColor(settings.ErrorColor);
        }

        embed.WithTitle(_localizer.FormatLocalized(settings.Locale, "search_result_description", Formatter.InlineCode(searchParameter)));

        foreach (var cmd in cmds.Chunk(AkkoConstants.LinesPerPage))
        {
            embed.AddField("command", string.Join("\n", cmd.Select(x => settings.Prefix + x.QualifiedName)), true);
            embed.AddField("description", string.Join("\n", cmd.Select(x => _localizer.FormatLocalized(settings.Locale, x.Description).MaxLength(47, AkkoConstants.EllipsisTerminator))), true);
        }

        return embed;
    }
}