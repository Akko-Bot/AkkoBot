using System;
using System.Collections.Generic;

namespace AkkoBot.Credential
{
    public class Credentials
    {
        public HashSet<ulong> OwnerIds { get; init; } = new() { 0 };
        public string Token { get; init; } = "paste_your_token_here";
        public bool EnableDms { get; set; } = true;
        public bool EnableMentionPrefix { get; set; } = false;
        public bool EnableHelpCommand { get; set; } = true;
        public Dictionary<string, string> Database { get; init; } = new()
        {
            { "Role", Environment.UserName },
            { "Password", "postgres_password_here" }
        };
    }
}