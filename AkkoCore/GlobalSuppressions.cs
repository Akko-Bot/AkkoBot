// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Style", "IDE0053:Use expression body for lambda expressions", Justification = "Ternary would impair readability.", Scope = "member", Target = "~F:AkkoCore.Commands.Formatters.CommandPlaceholders.parameterizedActions")]
[assembly: SuppressMessage("Style", "IDE0046:Convert to conditional expression", Justification = "Ternary would impair readability.", Scope = "member", Target = "~M:AkkoCore.Services.Localization.AkkoLocalizer.GetResponseString(System.String,System.String)~System.String")]
[assembly: SuppressMessage("Style", "IDE0046:Convert to conditional expression", Justification = "Ternary would impair readability.", Scope = "member", Target = "~M:AkkoCore.Services.Events.TagEventHandler.FilterEmojiTags(System.String,DSharpPlus.EventArgs.MessageCreateEventArgs,AkkoCore.Services.Database.Entities.TagEntity)~System.Boolean")]
[assembly: SuppressMessage("Style", "IDE0046:Convert to conditional expression", Justification = "Ternary would impair readability.", Scope = "member", Target = "~M:AkkoCore.Services.Events.TagEventHandler.FilterTags(System.String,DSharpPlus.EventArgs.MessageCreateEventArgs,AkkoCore.Services.Database.Entities.TagEntity)~System.Boolean")]
[assembly: SuppressMessage("Style", "IDE0046:Convert to conditional expression", Justification = "Ternary would impair readability.", Scope = "member", Target = "~M:AkkoCore.Extensions.DiscordChannelExt.GetMessagesAsync(DSharpPlus.Entities.DiscordChannel,DSharpPlus.DiscordClient,System.Int32)~System.Threading.Tasks.Task{System.Collections.Generic.IEnumerable{DSharpPlus.Entities.DiscordMessage}}")]