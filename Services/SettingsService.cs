using System.Text.Json;
using System.IO;

namespace MyCap.Services
{
    public class AppSettings
    {
        public string SaveDirectory { get; set; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
            "MyCap");

        public bool AutoSave { get; set; } = true;
        public string DefaultFormat { get; set; } = "png";
        public Dictionary<string, string> Hotkeys { get; set; } = new()
        {
            { "FullScreen", "F11" },
            { "RegionSelect", "F12" },
            { "WindowCapture", "F10" }
        };
    }

    public class SettingsService
    {
        private readonly string settingsPath;
        private AppSettings currentSettings = new();

        public SettingsService()
        {
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "MyCap");

            Directory.CreateDirectory(appDataPath);
            settingsPath = Path.Combine(appDataPath, "settings.json");

            LoadSettings();
        }

        public AppSettings Settings => currentSettings;

        private void LoadSettings()
        {
            if (File.Exists(settingsPath))
            {
                var json = File.ReadAllText(settingsPath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json);
                if (settings != null)
                {
                    currentSettings = settings;
                }
            }

            // Ensure save directory exists
            Directory.CreateDirectory(currentSettings.SaveDirectory);
        }

        public void SaveSettings()
        {
            var json = JsonSerializer.Serialize(currentSettings, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(settingsPath, json);
        }
    }
} 