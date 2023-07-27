// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

// <SettingsSnippet>
using Microsoft.Extensions.Configuration;

namespace PartsInventoryConnector;

public class Settings
{
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string? TenantId { get; set; }

    public static Settings LoadSettings()
    {
        // Load settings
        IConfiguration config = new ConfigurationBuilder()
            .AddUserSecrets<Program>()
            .Build();

        return config.GetRequiredSection("Settings").Get<Settings>() ??
            throw new Exception("Could not load app settings. See README for configuration instructions.");
    }
}
// </SettingsSnippet>
