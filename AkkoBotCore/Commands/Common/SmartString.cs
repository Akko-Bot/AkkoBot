using AkkoBot.Commands.Abstractions;
using AkkoBot.Commands.Formatters;
using AkkoBot.Services.Caching.Abstractions;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Text;
using System.Text.RegularExpressions;

namespace AkkoBot.Commands.Common
{
    /// <summary>
    /// Represents a string that automatically replaces placeholders matched by a regex with values from a formatter.
    /// </summary>
    public class SmartString
    {
        private readonly bool _sanitizeRoles;
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
        /// The current content of this <see cref="SmartString"/>.
        /// </summary>
        public string Content
        {
            get
            {
                if (!IsParsed)
                {
                    ParsePlaceholders();

                    if (_sanitizeRoles)
                        SanitizeRoleMentions();

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
        /// <param name="formatter">The object responsible for converting the placeholders to the values they represent. Default is <see cref="CommandPlaceholders"/>.</param>
        public SmartString(CommandContext context, string content, bool sanitizeRoles = false, Regex regex = null, IPlaceholderFormatter formatter = null)
        {
            _context = context;
            _contentBuilder = new(content ?? context.RawArgumentString);
            _sanitizeRoles = sanitizeRoles;
            ParseRegex = regex ?? _defaultPlaceholderRegex;
            _formatter = formatter ?? context.CommandsNext.Services.GetService<CommandPlaceholders>();
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
        /// Parses the placeholders from the user input into the string value they represent.
        /// </summary>
        /// <returns><see langword="true"/> if a placeholder was found in the user input, <see langword="false"/> otherwise.</returns>
        private bool ParsePlaceholders()
        {
            var matches = ParseRegex.Matches(_contentBuilder.ToString());

            if (matches.Count == 0)
                return false;

            foreach (Match match in matches)
            {
                if (_formatter.TryParse(_context, match, out var result) && result is not null)
                    _contentBuilder.Replace(match.ToString(), result.ToString());
            }

            return true;
        }

        /// <summary>
        /// Removes role mentions for roles that are above the user in the hierarchy.
        /// </summary>
        /// <returns><see langword="true"/> if a role mention was found, <see langword="false"/> otherwise.</returns>
        private bool SanitizeRoleMentions()
        {
            var matches = _roleRegex.Matches(_contentBuilder.ToString());

            if (_context.Guild is null || matches.Count == 0)
                return false;

            var canMentionAll = _context.Member.PermissionsIn(_context.Channel).HasPermission(Permissions.MentionEveryone);

            // If user is not server owner, admin or has no permission to mention everyone, remove everyone mentions from the message
            if (_context.Member.Hierarchy != int.MaxValue && !canMentionAll)
            {
                _contentBuilder.Replace("@everyone", Formatter.InlineCode("@everyone"));
                _contentBuilder.Replace("@here", Formatter.InlineCode("@here"));
            }

            if (_context.Services.GetService<IDbCache>().Guilds[_context.Guild.Id].PermissiveRoleMention)
            {
                // Sanitize by role hierarchy - Permissive
                foreach (Match match in matches)
                {
                    if (ulong.TryParse(match.Groups[1].Value, out var rid)
                        && _context.Guild.Roles.TryGetValue(rid, out var role)
                        && !role.IsMentionable
                        && _context.Member.Hierarchy <= role.Position)
                        _contentBuilder.Replace(match.Groups[0].Value, $"@{role.Name}");
                }
            }
            else
            {
                // Sanitize by mention everyone permission - Strict
                foreach (Match match in matches)
                {
                    if (ulong.TryParse(match.Groups[1].Value, out var rid)
                        && _context.Guild.Roles.TryGetValue(rid, out var role)
                        && !role.IsMentionable
                        && !canMentionAll)
                        _contentBuilder.Replace(match.Groups[0].Value, $"@{role.Name}");
                }
            }

            return true;
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

        public static implicit operator Optional<string>(SmartString x) => x?.Content;

        /* Overrides */

        public override string ToString()
            => Content;

        public override bool Equals(object obj)
            => (ReferenceEquals(this, obj) || obj is not null) && Content == obj.ToString();

        public override int GetHashCode()
            => base.GetHashCode();
    }
}