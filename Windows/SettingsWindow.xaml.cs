using System.Windows;
using System.Windows.Forms;
using MyCap.Services;

namespace MyCap.Windows
{
    public partial class SettingsWindow : Window
    {
        private readonly SettingsService settingsService;
        private readonly AppSettings originalSettings;

        public SettingsWindow(SettingsService settingsService)
        {
            InitializeComponent();
            this.settingsService = settingsService;
            originalSettings = new AppSettings
            {
                SaveDirectory = settingsService.Settings.SaveDirectory,
                AutoSave = settingsService.Settings.AutoSave,
                DefaultFormat = settingsService.Settings.DefaultFormat,
                Hotkeys = new Dictionary<string, string>(settingsService.Settings.Hotkeys)
            };

            LoadSettings();
        }

        private void LoadSettings()
        {
            SaveDirectoryTextBox.Text = settingsService.Settings.SaveDirectory;
            AutoSaveCheckBox.IsChecked = settingsService.Settings.AutoSave;

            FullScreenHotkeyTextBox.Text = settingsService.Settings.Hotkeys["FullScreen"];
            RegionSelectHotkeyTextBox.Text = settingsService.Settings.Hotkeys["RegionSelect"];
            WindowCaptureHotkeyTextBox.Text = settingsService.Settings.Hotkeys["WindowCapture"];
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = "Select Save Directory",
                SelectedPath = SaveDirectoryTextBox.Text
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                SaveDirectoryTextBox.Text = dialog.SelectedPath;
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            settingsService.Settings.SaveDirectory = SaveDirectoryTextBox.Text;
            settingsService.Settings.AutoSave = AutoSaveCheckBox.IsChecked ?? false;

            settingsService.Settings.Hotkeys["FullScreen"] = FullScreenHotkeyTextBox.Text;
            settingsService.Settings.Hotkeys["RegionSelect"] = RegionSelectHotkeyTextBox.Text;
            settingsService.Settings.Hotkeys["WindowCapture"] = WindowCaptureHotkeyTextBox.Text;

            settingsService.SaveSettings();
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // Restore original settings
            settingsService.Settings.SaveDirectory = originalSettings.SaveDirectory;
            settingsService.Settings.AutoSave = originalSettings.AutoSave;
            settingsService.Settings.DefaultFormat = originalSettings.DefaultFormat;
            settingsService.Settings.Hotkeys = new Dictionary<string, string>(originalSettings.Hotkeys);

            Close();
        }
    }
} 