using AkkoCore.Commands.Abstractions;
using AkkoCore.Services.Caching.Abstractions;
using AkkoCore.Services.Database.Enums;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Text;
using System.Text.RegularExpressions;

namespace AkkoCore.Commands.Common
{
    /// <summary>
    /// Represents a string that automatically replaces placeholders matched by a regex with values from a formatter.
    /// </summary>
    public sealed class SmartString
    {
        private static readonly Regex _roleRegex = new(@"<@&(\d+?)>", RegexOptions.Compiled);
        private static readonly Regex _defaultPlaceholderRegex = new(@"{([\w\.]+)\((.+?)\)}|{([\w\.]+)}", RegexOptions.Compiled);
        private readonly StringBuilder _contentBuilder;
        private readonly CommandContext _context;
        private readonly IPlaceholderFormatter _formatter;
        private string _parsedContent;

        /// <summary>
        /// Defines the regex to match the placeholders.
        /// </summary>
        public Regex ParseRegex { get; set; }

        /// <summary>
        /// Reports whether this <see cref="SmartString"/> had its content processed.
        /// </summary>
        public bool IsParsed { get; private set; }

        /// <summary>
        /// Determines whether roles should be sanitized or not.
        /// </summary>
        public bool SanitizeRoles { get; set; }

        /// <summary>
        /// Gets the number of characters of this <see cref="SmartString"/>.
        /// </summary>
        /// <remarks>This forces the string to be parsed if it hasn't been already.</remarks>
        public int Length
            => Content.Length;

        /// <summary>
        /// The current content of this <see cref="SmartString"/>.
        /// </summary>
        public string Content
        {
            get
            {
                if (!IsParsed)
                {
                    ParsePlaceholders(_context, _contentBuilder, ParseRegex, _formatter);

                    if (SanitizeRoles)
                        SanitizeRoleMentions(_context, _contentBuilder, _roleRegex);

                    IsParsed = true;

                    // Save the final result, then clear the builder.
                    _parsedContent = _contentBuilder.ToString();
                    _contentBuilder.Clear();
                }

                return _parsedContent;
            }

            set
            {
                _contentBuilder.Clear();

                if (string.IsNullOrWhiteSpace(value))
                {
                    _parsedContent = value;
                    return;
                }

                _contentBuilder.Append(value);
                IsParsed = false;
            }
        }

        /// <summary>
        /// Constructs a <see cref="SmartString"/> that automatically replaces placeholders matched by a regex with values from a formatter.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="content">The text with placeholders in it.</param>
        /// <param name="sanitizeRoles">Defines whether role mentions should be sanitized or not.</param>
        /// <param name="regex">The regex to match the placeholders in <paramref name="content"/>. Default is "{([\w\.]+)\((.+?)\)}|{([\w\.]+)}".</param>
        /// <param name="formatter">The object responsible for converting the placeholders to the values they represent.</param>
        public SmartString(CommandContext context, string content, bool sanitizeRoles = false, Regex regex = null, IPlaceholderFormatter formatter = null)
        {
            _context = context;
            _contentBuilder = new(content ?? context.RawArgumentString);
            SanitizeRoles = sanitizeRoles;
            ParseRegex = regex ?? _defaultPlaceholderRegex;
            _formatter = formatter ?? context.CommandsNext.Services.GetRequiredService<IPlaceholderFormatter>();
        }

        /// <summary>
        /// Determines whether this <see cref="SmartString"/> and a specified <see langword="string"/> object have the same value. A parameter specifies the culture, case, and sort rules used in the comparison.
        /// </summary>
        /// <param name="text">The string to compare to this instance.</param>
        /// <param name="comparisonType">One of the enumeration values that specifies how the strings will be compared.</param>
        /// <returns><see langword="true"/> if the value of the value parameter is the same as this <see cref="SmartString"/>, <see langword="false"/> otherwise.</returns>
        public bool Equals(string text, StringComparison comparisonType)
            => Content.Equals(text, comparisonType);

        /// <summary>
        /// Replaces all occurrences of a specified string in this instance with another specified string.
        /// </summary>
        /// <param name="oldValue">The string to be replaced.</param>
        /// <param name="newValue">The replacement string.</param>
        /// <returns>A reference to this instance with all instances of <paramref name="oldValue"/> replaced by <paramref name="newValue"/>.</returns>
        public SmartString Replace(string oldValue, string newValue)
        {
            if (IsParsed)
                _contentBuilder.Append(_parsedContent);

            _contentBuilder.Replace(oldValue, newValue ?? string.Empty);
            IsParsed = false;

            return this;
        }

        /// <summary>
        /// Parses the specified string without changing the internal state of this <see cref="SmartString"/>.
        /// </summary>
        /// <param name="input">The string to be parsed.</param>
        /// <param name="context">The command context.</param>
        /// <param name="sanitizeRoles">
        /// <see langword="true"/> to sanitize roles, <see langword="false"/> to not sanitize roles or <see langword="null"/>
        /// to use the setting of this <see cref="SmartString"/>.
        /// </param>
        /// <param name="placeholderRegex">
        /// The regex to match the command placeholders or <see langword="null"/>
        /// to use the regex set in this <see cref="SmartString"/>.
        /// </param>
        /// <param name="formatter">
        /// The formatter to parse the command placeholders or <see langword="null"/>
        /// to use the formatter set in this <see cref="SmartString"/>.
        /// </param>
        /// <returns>The parsed <paramref name="input"/>.</returns>
        public string Parse(string input, CommandContext context = default, bool? sanitizeRoles = default, Regex placeholderRegex = default, IPlaceholderFormatter formatter = default)
        {
            context ??= _context;
            placeholderRegex ??= ParseRegex;
            formatter ??= context.Services.GetRequiredService<IPlaceholderFormatter>();

            var result = new StringBuilder(input);

            result = ParsePlaceholders(context, result, placeholderRegex, formatter);

            if (sanitizeRoles ?? SanitizeRoles)
                result = SanitizeRoleMentions(context, result, _roleRegex);

            return result.ToString();
        }

        /// <summary>
        /// Parses the specified string for command placeholders.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="input">The string to be parsed.</param>
        /// <param name="sanitizeRoles"><see langword="true"/> to sanitize roles, <see langword="false"/> to not sanitize roles.</param>
        /// <param name="placeholderRegex">The regex to match the command placeholders or <see langword="null"/> to use the default regex.</param>
        /// <param name="formatter">The formatter to parse the command placeholders or <see langword="null"/> to use the default formatter.</param>
        /// <returns>The parsed <paramref name="input"/>.</returns>
        public static string Parse(CommandContext context, string input, bool sanitizeRoles = false, Regex placeholderRegex = default, IPlaceholderFormatter formatter = default)
        {
            if (context is null || input is null)
                throw new ArgumentNullException((context is null) ? nameof(context) : nameof(input), "Command context and input cannot be null.");

            placeholderRegex ??= _defaultPlaceholderRegex;
            formatter ??= context.Services.GetRequiredService<IPlaceholderFormatter>();

            var result = new StringBuilder(input);

            result = ParsePlaceholders(context, result, placeholderRegex, formatter);

            if (sanitizeRoles)
                result = SanitizeRoleMentions(context, result, _roleRegex);

            return result.ToString();
        }

        /// <summary>
        /// Parses the placeholders from the user input into the string value they represent.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="contentBuilder">The string builder that contains the input to be processed.</param>
        /// <param name="placeholderRegex">The regex for matching command placeholders.</param>
        /// <param name="formatter">The formatter that parses command placeholders.</param>
        /// <returns>The processed <paramref name="contentBuilder"/></returns>
        private static StringBuilder ParsePlaceholders(CommandContext context, StringBuilder contentBuilder, Regex placeholderRegex, IPlaceholderFormatter formatter)
        {
            var matches = placeholderRegex.Matches(contentBuilder.ToString());

            if (matches.Count == 0)
                return contentBuilder;

            foreach (Match match in matches)
            {
                if (formatter.TryParse(context, match, out var result) && result is not null)
                    contentBuilder.Replace(match.ToString(), result.ToString());
            }

            return contentBuilder;
        }

        /// <summary>
        /// Removes role mentions for roles that are above the user in the hierarchy.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="contentBuilder">The string builder that contains the input to be processed.</param>
        /// <param name="roleRegex">The regex for matching Discord roles.</param>
        /// <returns>The processed <paramref name="contentBuilder"/>.</returns>
        private static StringBuilder SanitizeRoleMentions(CommandContext context, StringBuilder contentBuilder, Regex roleRegex)
        {
            var matches = roleRegex.Matches(contentBuilder.ToString());

            if (context.Guild is null || matches.Count == 0)
                return contentBuilder;

            var canMentionAll = context.Member.PermissionsIn(context.Channel).HasPermission(Permissions.MentionEveryone);

            // If user is not server owner, admin or has no permission to mention everyone, remove everyone mentions from the message
            if (context.Member.Hierarchy != int.MaxValue && !canMentionAll)
            {
                contentBuilder.Replace("@everyone", Formatter.InlineCode("@everyone"));
                contentBuilder.Replace("@here", Formatter.InlineCode("@here"));
            }

            if (context.Services.GetRequiredService<IDbCache>().Guilds[context.Guild.Id].Behavior.HasFlag(GuildConfigBehavior.PermissiveRoleMention))
            {
                // Sanitize by role hierarchy - Permissive
                foreach (Match match in matches)
                {
                    if (ulong.TryParse(match.Groups[1].Value, out var rid)
                        && context.Guild.Roles.TryGetValue(rid, out var role)
                        && !role.IsMentionable
                        && context.Member.Hierarchy <= role.Position)
                        contentBuilder.Replace(match.Groups[0].Value, $"@{role.Name}");
                }
            }
            else
            {
                // Sanitize by mention everyone permission - Strict
                foreach (Match match in matches)
                {
                    if (ulong.TryParse(match.Groups[1].Value, out var rid)
                        && context.Guild.Roles.TryGetValue(rid, out var role)
                        && !role.IsMentionable
                        && !canMentionAll)
                        contentBuilder.Replace(match.Groups[0].Value, $"@{role.Name}");
                }
            }

            return contentBuilder;
        }

        /* Operator Overloads */

        public static string operator *(SmartString x, int y)
        {
            if (y <= 0)
                return string.Empty;
            else if (y > 1)
            {
                var temp = new StringBuilder();
                var counter = 0;

                while (counter++ < y)
                    temp.Append(x.Content);

                return temp.ToString();
            }

            return x.Content;
        }

        public static string operator +(SmartString x, SmartString y) => x.Content + y.Content;

        public static string operator +(SmartString x, string y) => x.Content + y;

        public static string operator +(string x, SmartString y) => x + y.Content;

        public static bool operator ==(SmartString x, SmartString y) => x.Content == y.Content;

        public static bool operator !=(SmartString x, SmartString y) => x.Content != y.Content;

        public static implicit operator string(SmartString x) => x?.Content;

        public static implicit operator Optional<string>(SmartString x) => x?.Content ?? Optional.FromNoValue<string>();

        /* Overrides */

        public override string ToString()
            => Content;

        public override bool Equals(object obj)
            => (ReferenceEquals(this, obj) || obj is not null) && Content == obj.ToString();

        public override int GetHashCode()
            => base.GetHashCode();
    }
}