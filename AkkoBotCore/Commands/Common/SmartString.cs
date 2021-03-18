﻿using AkkoBot.Commands.Abstractions;
using AkkoBot.Commands.Formatters;
using DSharpPlus.CommandsNext;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Text;
using System.Text.RegularExpressions;

namespace AkkoBot.Commands.Common
{
    public class SmartString
    {
        private readonly StringBuilder _parsedContent;
        private readonly CommandContext _context;
        private readonly IPlaceholderFormatter _formatter;
        
        /// <summary>
        /// Defines the regex to match the placeholders.
        /// </summary>
        public Regex ParseRegex { get; set; }

        /// <summary>
        /// Defines whether this <see cref="SmartString"/> had the placeholders in its content parsed.
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
                    IsParsed = ParseUserInput();

                return _parsedContent.ToString();
            }

            set
            {
                _parsedContent.Clear();
                _parsedContent.Append(value ?? string.Empty);
                IsParsed = false;
            }
        }

        /// <summary>
        /// Constructs a <see cref="SmartString"/> that automatically replaces placeholders matched by a regex with values from a formatter.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="content">The text with placeholders in it.</param>
        /// <param name="regex">The regex to match the placeholders in <paramref name="content"/>. Default is "{(.*?)}".</param>
        /// <param name="formatter">The object responsible for converting the placeholders to the values they represent. Default is <see cref="AkkoPlaceholders"/>.</param>
        public SmartString(CommandContext context, string content, Regex regex = null, IPlaceholderFormatter formatter = null)
        {
            _context = context;
            _parsedContent = new(content);
            ParseRegex = regex ?? new Regex("{(.*?)}");
            _formatter = formatter ?? context.CommandsNext.Services.GetService<AkkoPlaceholders>();
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
        private bool ParseUserInput()
        {
            var matches = ParseRegex.Matches(_parsedContent.ToString());
            
            if (matches.Count == 0)
                return false;

            foreach (Match match in matches)
            {
                if (_formatter.Parse(_context, match.Groups[1].ToString(), out var parsedPh) is not null)
                    _parsedContent.Replace(match.ToString(), parsedPh);
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

        /* Overrides */

        public override string ToString()
            => Content;

        public override bool Equals(object obj) 
            => (ReferenceEquals(this, obj) || obj is not null) && Content == obj.ToString();

        public override int GetHashCode() 
            => base.GetHashCode();
    }
}