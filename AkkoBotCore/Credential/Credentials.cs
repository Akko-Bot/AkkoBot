using System;
using System.Collections.Generic;

namespace AkkoBot.Credential
{
    /// <summary>
    /// A class that represents a credentials file.
    /// </summary>
    public class Credentials
    {
        public HashSet<ulong> OwnerIds { get; init; } = new() { 0 };
        public string Token { get; init; } = "paste_your_token_here";
        public Dictionary<string, string> Database { get; init; } = new(2)
        {
            { "Role", Environment.UserName },
            { "Password", "postgres_password_here" }
        };
    }
}