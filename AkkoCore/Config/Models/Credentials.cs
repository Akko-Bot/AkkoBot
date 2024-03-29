﻿using ConcurrentCollections;
using System;
using System.Collections.Generic;

namespace AkkoCore.Config.Models;

/// <summary>
/// A class that represents a credentials file.
/// </summary>
public sealed class Credentials
{
    /// <summary>
    /// Contains the IDs of the bot owners.
    /// </summary>
    public ConcurrentHashSet<ulong> OwnerIds { get; init; } = new() { default };

    /// <summary>
    /// The token used to connect to Discord.
    /// </summary>
    public string Token { get; init; } = "paste_your_token_here";

    /// <summary>
    /// Database credentials.
    /// </summary>
    public Dictionary<string, string> Database { get; init; } = new()
    {
        { "role", Environment.UserName },
        { "password", "postgres_password_here" },
        { "custom_connection_string", null! }
    };
}