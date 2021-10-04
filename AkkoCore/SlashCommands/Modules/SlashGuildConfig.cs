//using AkkoCore.Extensions;
//using AkkoCore.Services.Events.Controllers.Abstractions;
//using AkkoCore.SlashCommands.Abstractions;
//using DSharpPlus;
//using DSharpPlus.Entities;
//using DSharpPlus.SlashCommands;
//using DSharpPlus.SlashCommands.Attributes;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace AkkoCore.SlashCommands.Modules
//{
//    [SlashModuleLifespan(SlashModuleLifespan.Singleton)]
//    public class SlashGuildConfig : AkkoSlashCommandModule
//    {
//        private readonly IGuildConfigController _controller;

//        public SlashGuildConfig(IGuildConfigController controller)
//            => _controller = controller;

//        [SlashCommand("settings", "Changes the settings for the bot in this server.")]
//        [SlashRequireGuild, SlashRequireUserPermissions(Permissions.ManageGuild)]
//        public async Task SlashGuildConfigAsync(InteractionContext context)
//            => await context.RespondLocalizedAsync(await _controller.InitialResponseAsync(context.Guild));
//    }
//}