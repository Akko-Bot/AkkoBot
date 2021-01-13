using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace AkkoBot.Services.Database.Abstractions
{
    public abstract class DbEntity
    {
        [Required]
        public DateTimeOffset DateAdded { get; init; } = DateTimeOffset.Now;
        //     = DateTimeOffset.ParseExact(
        //         DateTimeOffset.Now.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fffffffzzz"),
        //         "o",
        //         CultureInfo.InvariantCulture.DateTimeFormat
        //);
    }
}
