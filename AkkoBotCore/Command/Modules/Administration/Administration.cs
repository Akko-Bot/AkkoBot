using AkkoBot.Command.Abstractions;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System.Threading.Tasks;

namespace AkkoBot.Command.Modules.Administration
{
    public partial class Administration : AkkoCommandModule
    {
        [Command("test")]
        public async Task Test(CommandContext context)
        {
            await context.RespondAsync("aaa");
        }
    }
}
