using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PDFSplitterforCopilot
{
    internal static class OpenAISettingsService
    {
        public const string DefaultModel = "gpt-4o-mini";

        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

        public static string SettingsPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PDFSplitterforCopilot",
            "openai-settings.json");

        public static string? GetApiKey()
        {
            string? environmentValue = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            if (!string.IsNullOrWhiteSpace(environmentValue))
            {
                return environmentValue;
            }

            var settings = LoadSettings();
            string? storedApiKey = Unprotect(settings?.EncryptedApiKey);
            if (!string.IsNullOrWhiteSpace(storedApiKey))
            {
                return storedApiKey;
            }

            return DotEnvConfig.GetValue("OPENAI_API_KEY");
        }

        public static string GetModel()
        {
            string? environmentValue = Environment.GetEnvironmentVariable("OPENAI_CONTEXT_SPLIT_MODEL");
            if (!string.IsNullOrWhiteSpace(environmentValue))
            {
                return environmentValue;
            }

            var settings = LoadSettings();
            if (!string.IsNullOrWhiteSpace(settings?.Model))
            {
                return settings.Model;
            }

            string? dotEnvValue = DotEnvConfig.GetValue("OPENAI_CONTEXT_SPLIT_MODEL");
            return string.IsNullOrWhiteSpace(dotEnvValue) ? DefaultModel : dotEnvValue;
        }

        public static bool HasApiKey()
        {
            return !string.IsNullOrWhiteSpace(GetApiKey());
        }

        public static bool HasStoredApiKey()
        {
            return !string.IsNullOrWhiteSpace(Unprotect(LoadSettings()?.EncryptedApiKey));
        }

        public static string GetStoredOrDefaultModel()
        {
            return LoadSettings()?.Model ?? GetModel();
        }

        public static void Save(string? apiKey, string? model)
        {
            var settings = LoadSettings() ?? new OpenAIUserSettings();

            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                settings.EncryptedApiKey = Protect(apiKey.Trim());
            }

            settings.Model = string.IsNullOrWhiteSpace(model) ? DefaultModel : model.Trim();
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(settings, JsonOptions), Encoding.UTF8);
        }

        public static void ClearStoredApiKey()
        {
            var settings = LoadSettings() ?? new OpenAIUserSettings();
            settings.EncryptedApiKey = null;
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(settings, JsonOptions), Encoding.UTF8);
        }

        private static OpenAIUserSettings? LoadSettings()
        {
            if (!File.Exists(SettingsPath))
            {
                return null;
            }

            try
            {
                return JsonSerializer.Deserialize<OpenAIUserSettings>(File.ReadAllText(SettingsPath));
            }
            catch
            {
                return null;
            }
        }

        private static string Protect(string value)
        {
            byte[] plainBytes = Encoding.UTF8.GetBytes(value);
            byte[] protectedBytes = ProtectedData.Protect(plainBytes, null, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(protectedBytes);
        }

        private static string? Unprotect(string? protectedValue)
        {
            if (string.IsNullOrWhiteSpace(protectedValue))
            {
                return null;
            }

            try
            {
                byte[] protectedBytes = Convert.FromBase64String(protectedValue);
                byte[] plainBytes = ProtectedData.Unprotect(protectedBytes, null, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(plainBytes);
            }
            catch
            {
                return null;
            }
        }

        private sealed class OpenAIUserSettings
        {
            public string? EncryptedApiKey { get; set; }
            public string? Model { get; set; }
        }
    }
}
