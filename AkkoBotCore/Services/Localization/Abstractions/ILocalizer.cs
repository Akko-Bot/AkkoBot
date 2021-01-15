using System.Collections.Generic;
using System.Globalization;

namespace AkkoBot.Services.Localization.Abstractions
{
    public interface ILocalizer
    {
        IEnumerable<string> GetAllLocales();
        string[] GetResponseStrings(CultureInfo locale, params string[] responses);
        string[] GetResponseStrings(string locale, params string[] responses);
        string GetResponseString(CultureInfo locale, string response);
        string GetResponseString(string locale, string response);
        void LoadLocalizedStrings();
        void ReloadLocalizedStrings();
    }
}