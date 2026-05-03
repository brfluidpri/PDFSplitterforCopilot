using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace PDFSplitterforCopilot
{
    internal static class DotEnvConfig
    {
        private static readonly object SyncRoot = new();
        private static Dictionary<string, string>? values;

        public static string? GetValue(string key)
        {
            string? environmentValue = Environment.GetEnvironmentVariable(key);
            if (!string.IsNullOrWhiteSpace(environmentValue))
            {
                return environmentValue;
            }

            EnsureLoaded();
            return values != null && values.TryGetValue(key, out string? value) && !string.IsNullOrWhiteSpace(value)
                ? value
                : null;
        }

        private static void EnsureLoaded()
        {
            if (values != null)
            {
                return;
            }

            lock (SyncRoot)
            {
                if (values != null)
                {
                    return;
                }

                values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                string? envPath = FindDotEnvPath();
                if (envPath == null)
                {
                    return;
                }

                foreach (string line in File.ReadLines(envPath))
                {
                    TryLoadLine(line, values);
                }
            }
        }

        private static string? FindDotEnvPath()
        {
            var candidates = new[]
            {
                Path.Combine(Directory.GetCurrentDirectory(), ".env"),
                Path.Combine(AppContext.BaseDirectory, ".env"),
                Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty, ".env")
            };

            foreach (string candidate in candidates)
            {
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            return null;
        }

        private static void TryLoadLine(string line, Dictionary<string, string> target)
        {
            string trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith("#", StringComparison.Ordinal))
            {
                return;
            }

            int equalsIndex = trimmed.IndexOf('=');
            if (equalsIndex <= 0)
            {
                return;
            }

            string key = trimmed.Substring(0, equalsIndex).Trim();
            string value = trimmed.Substring(equalsIndex + 1).Trim();
            value = Unquote(value);

            if (key.Length > 0)
            {
                target[key] = value;
            }
        }

        private static string Unquote(string value)
        {
            if (value.Length >= 2
                && ((value[0] == '"' && value[^1] == '"') || (value[0] == '\'' && value[^1] == '\'')))
            {
                return value.Substring(1, value.Length - 2);
            }

            return value;
        }
    }
}
