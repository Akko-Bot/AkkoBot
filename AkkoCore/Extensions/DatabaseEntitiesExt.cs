using AkkoCore.Commands.Common;
using AkkoCore.Services.Database.Entities;

namespace AkkoCore.Extensions
{
    public static class DatabaseEntitiesExt
    {
        /// <summary>
        /// Parses a command alias and its arguments into an actual command string.
        /// </summary>
        /// <param name="smartString">A smart string with the context where the command should run.</param>
        /// <param name="aliasInput">The raw command alias with arguments, if any.</param>
        /// <returns>The command string with the alias' arguments.</returns>
        public static string ParseAliasInput(this AliasEntity entity, SmartString smartString, string aliasInput)
        {
            smartString.Content = entity.Alias;
            var args = aliasInput.Replace(smartString, string.Empty).Trim();
            return (string.IsNullOrWhiteSpace(args)) ? entity.FullCommand : $"{entity.Command} {args}";
        }
    }
}