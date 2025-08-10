using System.Text.Json;
using System.IO;
using System.Windows.Input;

namespace MyCap.Services
{
    public class AppSettings
    {
        public string SaveDirectory { get; set; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
            "MyCap");

        public bool AutoSave { get; set; } = true;
        public bool AutoStart { get; set; } = true;
        public string DefaultFormat { get; set; } = "png";
        public bool QuietMode { get; set; } = false;
        
        // 저장된 영역 좌표
        public int? SavedRegionX { get; set; } = null;
        public int? SavedRegionY { get; set; } = null;
        public int? SavedRegionWidth { get; set; } = null;
        public int? SavedRegionHeight { get; set; } = null;
        
        // 모든 단축키 설정
        public Dictionary<string, KeyboardShortcut> Shortcuts { get; set; } = new()
        {
            { "FullScreen", new KeyboardShortcut { Key = Key.F2, Modifiers = ModifierKeys.Control } },
            { "RegionSelect", new KeyboardShortcut { Key = Key.F1, Modifiers = ModifierKeys.Control } },
            { "WindowCapture", new KeyboardShortcut { Key = Key.F3, Modifiers = ModifierKeys.Control } },
            { "SameRegionCapture", new KeyboardShortcut { Key = Key.F8, Modifiers = ModifierKeys.Control } },
            { "SaveAs", new KeyboardShortcut { Key = Key.S, Modifiers = ModifierKeys.Control | ModifierKeys.Shift } },
            { "Copy", new KeyboardShortcut { Key = Key.C, Modifiers = ModifierKeys.Control } },
            { "About", new KeyboardShortcut { Key = Key.F1, Modifiers = ModifierKeys.None } },
            { "CloseDialog", new KeyboardShortcut { Key = Key.Escape, Modifiers = ModifierKeys.None } },
            { "ExitApplication", new KeyboardShortcut { Key = Key.X, Modifiers = ModifierKeys.Control } },
            { "OpenSaveFolder", new KeyboardShortcut { Key = Key.F5, Modifiers = ModifierKeys.Control } }
        };

        // 기본 설정으로 초기화하는 메서드
        public void ResetToDefaults()
        {
            SaveDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
                "MyCap");
            AutoSave = true;
            AutoStart = true;
            DefaultFormat = "png";
            QuietMode = false;
            
            // 저장된 영역 좌표 초기화
            SavedRegionX = null;
            SavedRegionY = null;
            SavedRegionWidth = null;
            SavedRegionHeight = null;
            Shortcuts = new Dictionary<string, KeyboardShortcut>
            {
                { "FullScreen", new KeyboardShortcut { Key = Key.F2, Modifiers = ModifierKeys.Control } },
                { "RegionSelect", new KeyboardShortcut { Key = Key.F1, Modifiers = ModifierKeys.Control } },
                { "WindowCapture", new KeyboardShortcut { Key = Key.F3, Modifiers = ModifierKeys.Control } },
                { "SameRegionCapture", new KeyboardShortcut { Key = Key.F8, Modifiers = ModifierKeys.Control } },
                { "SaveAs", new KeyboardShortcut { Key = Key.S, Modifiers = ModifierKeys.Control | ModifierKeys.Shift } },
                { "Copy", new KeyboardShortcut { Key = Key.C, Modifiers = ModifierKeys.Control } },
                { "About", new KeyboardShortcut { Key = Key.F1, Modifiers = ModifierKeys.None } },
                { "CloseDialog", new KeyboardShortcut { Key = Key.Escape, Modifiers = ModifierKeys.None } },
                { "ExitApplication", new KeyboardShortcut { Key = Key.X, Modifiers = ModifierKeys.Control } },
                { "OpenSaveFolder", new KeyboardShortcut { Key = Key.F5, Modifiers = ModifierKeys.Control } }
            };
        }

        // 저장된 영역을 Rectangle로 반환하는 메서드
        public System.Drawing.Rectangle? GetSavedRegion()
        {
            if (SavedRegionX.HasValue && SavedRegionY.HasValue && 
                SavedRegionWidth.HasValue && SavedRegionHeight.HasValue)
            {
                return new System.Drawing.Rectangle(
                    SavedRegionX.Value, 
                    SavedRegionY.Value, 
                    SavedRegionWidth.Value, 
                    SavedRegionHeight.Value);
            }
            return null;
        }

        // 영역을 저장하는 메서드
        public void SetSavedRegion(System.Drawing.Rectangle region)
        {
            SavedRegionX = region.X;
            SavedRegionY = region.Y;
            SavedRegionWidth = region.Width;
            SavedRegionHeight = region.Height;
        }

        // 저장된 영역을 초기화하는 메서드
        public void ClearSavedRegion()
        {
            SavedRegionX = null;
            SavedRegionY = null;
            SavedRegionWidth = null;
            SavedRegionHeight = null;
        }
    }

    // 단축키 설정을 위한 클래스
    public class KeyboardShortcut
    {
        public Key Key { get; set; }
        public ModifierKeys Modifiers { get; set; }

        public KeyboardShortcut()
        {
            Key = Key.None;
            Modifiers = ModifierKeys.None;
        }

        public KeyboardShortcut(Key key, ModifierKeys modifiers)
        {
            Key = key;
            Modifiers = modifiers;
        }

        public KeyboardShortcut Clone()
        {
            return new KeyboardShortcut { Key = Key, Modifiers = Modifiers };
        }

        public override string ToString()
        {
            var modifierString = "";
            if ((Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                modifierString += "Ctrl + ";
            if ((Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt)
                modifierString += "Alt + ";
            if ((Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                modifierString += "Shift + ";

            return $"{modifierString}{Key}";
        }

        public static KeyboardShortcut Parse(string shortcutStr)
        {
            var parts = shortcutStr.Split('+');
            var shortcut = new KeyboardShortcut();
            
            if (parts.Length > 1)
            {
                // Parse modifiers
                var modifierStr = parts[0].Trim();
                if (Enum.TryParse<ModifierKeys>(modifierStr, out var modifiers))
                {
                    shortcut.Modifiers = modifiers;
                }
                
                // Parse key
                var keyStr = parts[^1].Trim();
                if (Enum.TryParse<Key>(keyStr, out var key))
                {
                    shortcut.Key = key;
                }
            }
            else
            {
                // Only key, no modifiers
                if (Enum.TryParse<Key>(shortcutStr.Trim(), out var key))
                {
                    shortcut.Key = key;
                    shortcut.Modifiers = ModifierKeys.None;
                }
            }
            
            return shortcut;
        }
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
            bool needToSave = false;

            try
            {
                if (File.Exists(settingsPath))
                {
                    var json = File.ReadAllText(settingsPath);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json);
                    
                    if (settings != null)
                    {
                        // 필수 설정값들이 있는지 확인
                        if (settings.Shortcuts == null || !ValidateShortcuts(settings.Shortcuts))
                        {
                            System.Diagnostics.Debug.WriteLine("Invalid shortcuts found, resetting to defaults");
                            settings.ResetToDefaults();
                            needToSave = true;
                        }

                        currentSettings = settings;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("Settings file is empty or invalid, creating new with defaults");
                        currentSettings.ResetToDefaults();
                        needToSave = true;
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Settings file not found, creating new with defaults");
                    currentSettings.ResetToDefaults();
                    needToSave = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading settings: {ex.Message}");
                currentSettings.ResetToDefaults();
                needToSave = true;
            }

            // 설정 파일이 없거나 잘못된 경우 새로 생성
            if (needToSave)
            {
                try
                {
                    SaveSettings();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error saving default settings: {ex.Message}");
                }
            }

            // 저장 디렉토리가 존재하는지 확인하고 없으면 생성
            try
            {
                Directory.CreateDirectory(currentSettings.SaveDirectory);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating save directory: {ex.Message}");
                // 저장 디렉토리 생성에 실패하면 기본 위치로 재설정
                currentSettings.SaveDirectory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
                    "MyCap");
                Directory.CreateDirectory(currentSettings.SaveDirectory);
            }
        }

        private bool ValidateShortcuts(Dictionary<string, KeyboardShortcut> shortcuts)
        {
            var requiredShortcuts = new[] 
            { 
                "FullScreen", "RegionSelect", "WindowCapture", "SameRegionCapture",
                "SaveAs", "Copy", "About", "CloseDialog", "ExitApplication", "OpenSaveFolder" 
            };

            return requiredShortcuts.All(key => shortcuts.ContainsKey(key));
        }

        public void SaveSettings()
        {
            try
            {
                var json = JsonSerializer.Serialize(currentSettings, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                File.WriteAllText(settingsPath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving settings: {ex.Message}");
                throw;
            }
        }

        // 설정을 기본값으로 초기화하는 메서드
        public void ResetToDefaults()
        {
            currentSettings.ResetToDefaults();
            SaveSettings();
        }
    }
} 