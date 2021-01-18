using System.Collections.Generic;
using AkkoBot.Extensions;

namespace AkkoBot.Services.Localization
{
    /// <summary>
    /// A model class for all possible response strings.
    /// </summary>
    public class LocalizedStrings
    {
        public string BlAdded { get; init; } = "Successfully added {0} {1} {2} to the blacklist.";
        public string BlClear { get; init; } = "{0} entries were removed from the blacklist successfully.";
        public string BlEmpty { get; init; } = "There are no entries in the blacklist.";
        public string BlExist { get; init; } = "{0} {1} {2} is blacklisted already.";
        public string BlNotExist { get; init; } = "{0} {1} {2} is not blacklisted.";
        public string BlRemoved { get; init; } = "Successfully removed {0} {1} {2} from the blacklist.";
        public string BlTitle { get; init; } = "Blacklist";
        public string Channel { get; init; } = "Channel";
        public string Days { get; init; } = "Days";
        public string Hours { get; init; } = "Hours";
        public string Id { get; init; } = "Id";
        public string Minutes { get; init; } = "Minutes";
        public string Name { get; init; } = "Name";
        public string OnlineSince { get; init; } = "Online since";
        public string Seconds { get; init; } = "Seconds";
        public string Server { get; init; } = "Server";
        public string Shutdown { get; init; } = "Shutting down.";
        public string Type { get; init; } = "Type";
        public string Unknown { get; init; } = "Unknown";
        public string Unspecified { get; init; } = "Unspecified";
        public string Uptime { get; init; } = "Uptime";
        public string User { get; init; } = "User";
        public string ErrorNotFound { get; init; } = "Error: the requested response string was not found.";

        /// <summary>
        /// Gets the collection of response strings stored in this object.
        /// </summary>
        /// <returns>
        /// A <see cref="Dictionary{string, string}"/> where the key is the property name in 
        /// snake_case format and the value is the response string.
        /// </returns>
        public IDictionary<string, string> GetStringCollection()
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