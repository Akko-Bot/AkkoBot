//using AkkoCore.Abstractions;
//using AkkoCore.Commands.Modules.Administration.Services;
//using AkkoCore.Common;
//using AkkoCore.Config.Models;
//using AkkoCore.Extensions;
//using AkkoCore.Services.Caching.Abstractions;
//using AkkoCore.Services.Database.Entities;
//using AkkoCore.Services.Database.Enums;
//using AkkoCore.Services.Events.Controllers.Abstractions;
//using AkkoCore.Services.Events.Controllers.Components;
//using AkkoCore.Services.Localization.Abstractions;
//using DSharpPlus.Entities;
//using System;
//using System.Linq;
//using System.Runtime.CompilerServices;
//using System.Threading.Tasks;
//using GCIT = AkkoCore.Services.Events.Controllers.Components.GuildConfigInteractionType;
//using GML = AkkoCore.Services.Events.Controllers.Components.GenericMenuLabels;

//namespace AkkoCore.Services.Events.Controllers
//{
//    internal sealed class GuildConfigController : IGuildConfigController
//    {
//        private const string _firstSelectMenuId = "menu_guildconfig1_update";
//        private const string _secondSelectMenuId = "menu_guildconfig2_update";
//        private static readonly string[] _acceptedIds = new[] { _firstSelectMenuId, _secondSelectMenuId, GenericMenuOptions.ExitButton.CustomId };

//        private readonly ILocalizer _localizer;
//        private readonly IDbCache _dbCache;
//        private readonly BotConfig _botConfig;
//        private readonly GuildConfigService _service;

//        public GuildConfigController(ILocalizer localizer, IDbCache dbCache, BotConfig botConfig, GuildConfigService service)
//        {
//            _localizer = localizer;
//            _dbCache = dbCache;
//            _botConfig = botConfig;
//            _service = service;
//        }

//        public async ValueTask<DiscordInteractionResponseBuilder> HandleRequestAsync(DiscordMessage message, string componentId, string[] options)
//        {
//            if (!componentId.EqualsAny(_acceptedIds))
//                return default;

//            var response = new DiscordInteractionResponseBuilder()
//                .AsEphemeral();

//            if (!GenericMenuOptions.ExitButton.CustomId.Equals(componentId, StringComparison.Ordinal))
//            {
//                var optionId = options?.FirstOrDefault();
//                var firstRow = GML.IsColor(optionId)                // If selected option was a color
//                    ? message.Components.First().Components.First() // Get the control from the original message
//                    : GetFirstDropMenu(optionId, message);          // Else, generate it ourselves

//                response.AddComponents(firstRow)
//                    .AddComponents(await GetSecondDropMenuAsync(message.Channel.Guild, optionId ?? componentId))
//                    .AddComponents(message.Components.Last().Components);
//            }

//            return response;
//        }

//        public async ValueTask<DiscordInteractionResponseBuilder> InitialResponseAsync(DiscordGuild server)
//        {
//            return new DiscordInteractionResponseBuilder()
//                .WithContent(AkkoConstants.ValidWhitespace)     // Discord responds with Bad Request if this is not here
//                .AddComponents(GetFirstDropMenu(string.Empty))
//                .AddComponents(await GetSecondDropMenuAsync(server))
//                .AddComponents(GetExitButton(server.Id))
//                .AsEphemeral();
//        }

//        private DiscordSelectComponent GetFirstDropMenu(string optionId, DiscordMessage message = null)
//        {
//            var sid = message?.Channel.GuildId;
//            var locale = GetSettings(sid).Locale;
//            var menuOptions = new DiscordSelectComponentOption[]
//            {
//                GetMenuOption(GCIT.EmbedId, GCIT.EmbedLabel, sid, IsEmbedMenu(optionId)),
//                GetMenuOption(GCIT.OkColorId, GCIT.OkColorLabel, sid, IsOkColorMenu(optionId)),
//                GetMenuOption(GCIT.ErrorColorId, GCIT.ErrorColorLabel, sid, IsErrorColorMenu(optionId))
//            };

//            return new(_firstSelectMenuId, _localizer.GetResponseString(locale, GML.SelectAnOptionLabel), menuOptions);
//        }

//        private async ValueTask<DiscordSelectComponent> GetSecondDropMenuAsync(DiscordGuild server, string componentId = default)
//        {
//            var settings = _service.GetGuildSettings(server);
//            var menuOptions = Enumerable.Empty<DiscordSelectComponentOption>()
//                .Append(GenericMenuOptions.Empty);

//            if (IsEmbedMenu(componentId))
//            {
//                var result = await _service.SetPropertyAsync(server, x =>
//                    x.Behavior = (GetEmbedConfig(settings, componentId))
//                        ? x.Behavior | GuildConfigBehavior.UseEmbed
//                        : x.Behavior & ~GuildConfigBehavior.UseEmbed
//                );

//                var toggled = (result.HasFlag(GuildConfigBehavior.UseEmbed) ? GML.EnableId : GML.DisableId);

//                menuOptions = GenericMenuOptions.Booleans.Select(x => x.WithLocalization(_localizer, settings.Locale, toggled == x.Value));
//            }

//            else if (IsColorMenu(componentId))
//            {
//                // This is where I stopped implementing this.
//                // Discord plans on adding  textbox components, which would benefit most options in GuildConfig,
//                // So there is no point in adding "/settings" until the API supports it
//                menuOptions = GenericMenuOptions.Colors.Select(x => x.WithLocalization(_localizer, settings.Locale, componentId == x.Value));

//            }

//            return new(_secondSelectMenuId, _localizer.GetResponseString(settings.Locale, GML.SelectAnOptionLabel), menuOptions, menuOptions.First().Equals(GenericMenuOptions.Empty));
//        }

//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        private DiscordSelectComponentOption GetMenuOption(string componentId, string description, ulong? sid, bool isDefault)
//        {
//            var locale = GetSettings(sid).Locale;
//            return new(_localizer.GetResponseString(locale, componentId), componentId, _localizer.GetResponseString(locale, description), isDefault);
//        }

//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        private DiscordButtonComponent GetExitButton(ulong? sid)
//        {
//            var locale = GetSettings(sid).Locale;
//            return GenericMenuOptions.ExitButton.WithLocalization(_localizer, locale);
//        }

//        /* Helper methods */
//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        private bool GetEmbedConfig(GuildConfigEntity settings, string componentId)
//            => GML.EnableId.Equals(componentId, StringComparison.Ordinal) || (!GML.DisableId.Equals(componentId, StringComparison.Ordinal) && settings.UseEmbed);

//        private string GetColorConfig(GuildConfigEntity settings, string componentId)
//        {
//            if (GCIT.OkColorId.Equals(componentId, StringComparison.Ordinal))
//                return GCIT.OkColorId;
//            else if (GCIT.ErrorColorId.Equals(componentId, StringComparison.Ordinal))
//                return GCIT.ErrorColorId;
//            else
//                return settings.
//        }

//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        private IMessageSettings GetSettings(ulong? sid)
//            => (_dbCache.Guilds.TryGetValue(sid ?? default, out var dbGuild)) ? dbGuild : _botConfig;

//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        private bool IsEmbedMenu(string optionId)
//            => GCIT.EmbedId.Equals(optionId, StringComparison.Ordinal) || GML.IsBoolean(optionId);

//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        private bool IsColorMenu(string optionId)
//            => GCIT.OkColorId.Equals(optionId, StringComparison.Ordinal) || GCIT.ErrorColorId.Equals(optionId, StringComparison.Ordinal) || GML.IsColor(optionId);

//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        private bool IsOkColorMenu(string optionId)
//            => !GCIT.ErrorColorId.Equals(optionId, StringComparison.Ordinal) && (GCIT.OkColorId.Equals(optionId, StringComparison.Ordinal) || GML.IsColor(optionId));

//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        private bool IsErrorColorMenu(string optionId)
//            => !GCIT.OkColorId.Equals(optionId, StringComparison.Ordinal) && (GCIT.ErrorColorId.Equals(optionId, StringComparison.Ordinal) || GML.IsColor(optionId));

//    }
//}