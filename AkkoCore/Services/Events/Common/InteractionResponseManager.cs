using AkkoCore.Services.Events.Abstractions;
using AkkoCore.Services.Events.Controllers.Abstractions;
using DSharpPlus.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AkkoCore.Services.Events.Common
{
    /// <summary>
    /// Generates interactive responses.
    /// </summary>
    internal sealed class InteractionResponseManager : IInteractionResponseManager
    {
        private readonly List<ISlashController> _slashControllers = new();

        //public InteractionResponseManager()
        //{
        //    _slashControllers = new();
        //    {
        //        controllers
        //    };
        //}

        public async ValueTask<DiscordInteractionResponseBuilder?> RequestAsync(DiscordMessage message, string componentId, string[] options)
        {
            foreach (var controller in _slashControllers)
            {
                var response = await controller.HandleRequestAsync(message, componentId, options);

                if (response is not null)
                    return response;
            }

            return default;
        }

        public void Add(ISlashController slashController)
            => _slashControllers.Add(slashController);

        public void AddRange(IEnumerable<ISlashController> slashControllers)
            => _slashControllers.AddRange(slashControllers);
    }
}