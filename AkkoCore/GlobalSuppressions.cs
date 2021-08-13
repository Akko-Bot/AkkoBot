// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Style", "IDE0019:Use pattern matching", Justification = "Causes scope issues in a switch-statement.", Scope = "member", Target = "~M:AkkoCore.Commands.Modules.Administration.Services.WarningService.ApplyPunishmentAsync(DSharpPlus.CommandsNext.CommandContext,DSharpPlus.Entities.DiscordUser,AkkoCore.Services.Database.Entities.WarnPunishEntity,System.String)~System.Threading.Tasks.Task")]
[assembly: SuppressMessage("Style", "IDE0053:Use expression body for lambda expressions", Justification = "Ternary would impair readability.", Scope = "member", Target = "~F:AkkoCore.Commands.Formatters.CommandPlaceholders.parameterizedActions")]
[assembly: SuppressMessage("Style", "IDE0046:Convert to conditional expression", Justification = "Ternary would impair readability.", Scope = "member", Target = "~M:AkkoCore.Services.Localization.AkkoLocalizer.GetResponseString(System.String,System.String)~System.String")]
