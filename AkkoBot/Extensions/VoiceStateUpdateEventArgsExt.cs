using AkkoCore.Enums;
using DSharpPlus.EventArgs;

namespace AkkoBot.Extensions
{
    public static class VoiceStateUpdateEventArgsExt
    {
        /// <summary>
        /// Gets the voice state of this voice event.
        /// </summary>
        /// <param name="eventArgs">This voice event.</param>
        /// <returns>The voice state of the user.</returns>
        public static UserVoiceState GetVoiceState(this VoiceStateUpdateEventArgs eventArgs)
        {
            return (eventArgs.Before == eventArgs.After)
                ? UserVoiceState.Reconnected
                : (eventArgs.Channel is null)
                    ? UserVoiceState.Disconnected
                    : (eventArgs.Before?.Channel is not null && eventArgs.After?.Channel is not null)
                        ? UserVoiceState.Moved
                        : UserVoiceState.Connected;
        }
    }
}