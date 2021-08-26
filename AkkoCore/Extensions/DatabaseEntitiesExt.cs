using AkkoCore.Services.Database.Entities;

namespace AkkoCore.Extensions
{
    public static class DatabaseEntitiesExt
    {
        /// <summary>
        /// Parses a command alias and its arguments into an actual command string.
        /// </summary>
        /// <param name="parsedAlias">The parsed alias.</param>
        /// <param name="aliasInput">The raw command alias with arguments, if any.</param>
        /// <returns>The command string with the alias' arguments.</returns>
        public static string ParseAliasInput(this AliasEntity entity, string parsedAlias, string aliasInput)
        {
            var args = aliasInput.Replace(parsedAlias, string.Empty).Trim();
            return (string.IsNullOrWhiteSpace(args)) ? entity.FullCommand : $"{entity.Command} {args}";
        }
    }
}