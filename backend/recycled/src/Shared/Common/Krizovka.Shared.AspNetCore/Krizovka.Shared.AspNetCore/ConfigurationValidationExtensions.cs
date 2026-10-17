using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace Krizovka.Shared.AspNetCore;

public static class ConfigurationValidationExtensions
{
    /// <summary>
    /// Ensures that the specified configuration keys are present and not empty.
    /// If any are missing, an InvalidOperationException is thrown.
    /// </summary>
    public static IConfiguration RequireValues(this IConfiguration configuration, params string[] keys)
    {
        var missingKeys = new List<string>();

        foreach (var key in keys)
        {
            var value = configuration[key];
            if (string.IsNullOrWhiteSpace(value))
            {
                missingKeys.Add(key);
            }
        }

        if (missingKeys.Any())
        {
            var joinedKeys = string.Join(", ", missingKeys);
            throw new InvalidOperationException(
                $"CRITICAL: Missing required configuration parameter(s): {joinedKeys}. " +
                $"Please set these variables in your .env file, secrets, or appsettings.json before starting the service.");
        }

        return configuration;
    }
}
