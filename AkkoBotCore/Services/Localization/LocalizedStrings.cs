using System.Collections.Generic;
using AkkoBot.Extensions;

namespace AkkoBot.Services.Localization
{
    /// <summary>
    /// A model class for all possible response strings.
    /// </summary>
    public class LocalizedStrings
    {
        public string ErrorNotFound { get; set; } = "Error: the requested response string was not found.";
        public string Shutdown { get; set; } = "Shutting down.";
        public string Uptime { get; set; } = "Uptime";
        public string Days { get; set; } = "Days";
        public string Hours { get; set; } = "Hours";
        public string Minutes { get; set; } = "Minutes";
        public string Seconds { get; set; } = "Seconds";

        /// <summary>
        /// Gets the collection of response strings stored in this object.
        /// </summary>
        /// <returns>
        /// A <see cref="Dictionary{string, string}"/> where the key is the property name in 
        /// snake_case format and the value is the response string.
        /// </returns>
        public Dictionary<string, string> GetStringCollection()
        {
            var props = this.GetType().GetProperties();
            var result = new Dictionary<string, string>(props.Length);

            foreach (var prop in props)
            {
                result.TryAdd(
                    prop.Name.ToSnakeCase(),
                    prop.GetValue(this).ToString()
                );
            }

            return result;
        }
    }
}