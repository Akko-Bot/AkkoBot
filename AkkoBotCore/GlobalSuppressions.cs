﻿// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Style", "IDE0053:Use expression body for lambda expressions", Justification = "Lambda would impair readability.", Scope = "member", Target = "~F:AkkoBot.Commands.Formatters.CommandPlaceholders.parameterizedActions")]
[assembly: SuppressMessage("Style", "IDE0046:Convert to conditional expression", Justification = "Ternary would impair readability.", Scope = "member", Target = "~M:AkkoBot.Services.Localization.AkkoLocalizer.GetResponseString(System.String,System.String)~System.String")]
[assembly: SuppressMessage("Style", "IDE0046:Convert to conditional expression", Justification = "Ternary would impair readability.", Scope = "member", Target = "~M:AkkoBot.Extensions.DiscordChannelExt.GetMessagesAsync(DSharpPlus.Entities.DiscordChannel,DSharpPlus.DiscordClient,System.Int32)~System.Threading.Tasks.Task{System.Collections.Generic.IEnumerable{DSharpPlus.Entities.DiscordMessage}}")]
[assembly: SuppressMessage("Style", "IDE0046:Convert to conditional expression", Justification = "Ternary would impair readability.", Scope = "member", Target = "~M:AkkoBot.Services.Events.VoiceRoleConnectionHandler.VoiceRoleAsync(DSharpPlus.DiscordClient,DSharpPlus.EventArgs.VoiceStateUpdateEventArgs)~System.Threading.Tasks.Task")]
[assembly: SuppressMessage("Style", "IDE0046:Convert to conditional expression", Justification = "Ternary would impair readability.", Scope = "member", Target = "~M:AkkoBot.Services.Events.GlobalEventsHandler.DefaultPrefixAsync(DSharpPlus.DiscordClient,DSharpPlus.EventArgs.MessageCreateEventArgs)~System.Threading.Tasks.Task")]
[assembly: SuppressMessage("Style", "IDE0046:Convert to conditional expression", Justification = "Ternary would impair readability.", Scope = "member", Target = "~M:AkkoBot.Services.Events.GlobalEventsHandler.HandleCommandAliasAsync(DSharpPlus.DiscordClient,DSharpPlus.EventArgs.MessageCreateEventArgs)~System.Threading.Tasks.Task")]
[assembly: SuppressMessage("Style", "IDE0046:Convert to conditional expression", Justification = "Ternary would impair readability.", Scope = "member", Target = "~M:AkkoBot.Services.Events.GuildEventsHandler.FilterWordAsync(DSharpPlus.DiscordClient,DSharpPlus.EventArgs.MessageCreateEventArgs)~System.Threading.Tasks.Task{System.Boolean}")]
[assembly: SuppressMessage("Style", "IDE0046:Convert to conditional expression", Justification = "Ternary would impair readability.", Scope = "member", Target = "~M:AkkoBot.Services.Events.GuildEventsHandler.FilterStickerAsync(DSharpPlus.DiscordClient,DSharpPlus.EventArgs.MessageCreateEventArgs)~System.Threading.Tasks.Task{System.Boolean}")]
[assembly: SuppressMessage("Style", "IDE0046:Convert to conditional expression", Justification = "Ternary would impair readability.", Scope = "member", Target = "~M:AkkoBot.Services.Events.GuildEventsHandler.PollVoteAsync(DSharpPlus.DiscordClient,DSharpPlus.EventArgs.MessageCreateEventArgs)~System.Threading.Tasks.Task")]
[assembly: SuppressMessage("Style", "IDE0046:Convert to conditional expression", Justification = "Ternary would impair readability.", Scope = "member", Target = "~M:AkkoBot.Services.Events.GuildLoadHandler.AddGuildOnJoinAsync(DSharpPlus.DiscordClient,DSharpPlus.EventArgs.GuildCreateEventArgs)~System.Threading.Tasks.Task")]
[assembly: SuppressMessage("Style", "IDE0019:Use pattern matching", Justification = "Causes scope issues in a switch-statement.", Scope = "member", Target = "~M:AkkoBot.Commands.Modules.Administration.Services.WarningService.ApplyPunishmentAsync(DSharpPlus.CommandsNext.CommandContext,DSharpPlus.Entities.DiscordUser,AkkoBot.Services.Database.Entities.WarnPunishEntity,System.String)~System.Threading.Tasks.Task")]
[assembly: SuppressMessage("Performance", "CA1826:Do not use Enumerable methods on indexable collections", Justification = "Collection can be empty, accessing it through the index may throw.", Scope = "member", Target = "~M:AkkoBot.Extensions.DiscordChannelExt.GetLatestMessageAsync(DSharpPlus.Entities.DiscordChannel,DSharpPlus.DiscordClient)~System.Threading.Tasks.Task{DSharpPlus.Entities.DiscordMessage}")]
[assembly: SuppressMessage("Performance", "CA1826:Do not use Enumerable methods on indexable collections", Justification = "Collection can be empty, accessing it through the index may throw.", Scope = "member", Target = "~M:AkkoBot.Commands.Modules.Self.OwnerCommands.SetAvatarAsync(DSharpPlus.CommandsNext.CommandContext)~System.Threading.Tasks.Task")]
