using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using MyCap.Services;
using MessageBox = System.Windows.MessageBox;
using Microsoft.Win32;
using System.Windows.Forms;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace MyCap.Windows
{
    public partial class SettingsWindow : Window
    {
        private readonly SettingsService _settingsService;
        private readonly ObservableCollection<HotkeyItem> _hotkeyItems;
        private readonly Dictionary<string, string> _shortcutNames = new Dictionary<string, string>
        {
            { "RegionSelect", "영역 캡처" },
            { "FullScreen", "전체 화면 캡처" },
            { "WindowCapture", "창 캡처" },
            { "SaveAs", "다른 이름으로 저장" },
            { "About", "정보" },
            { "CloseDialog", "대화상자 닫기" },
            { "ExitApplication", "프로그램 종료" },
            { "OpenSaveFolder", "저장폴더 열기" }
        };
        private const string StartupRegistryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        private const string AppName = "MyCap";

        public SettingsWindow(SettingsService settingsService)
        {
            InitializeComponent();
            _settingsService = settingsService;
            _hotkeyItems = new ObservableCollection<HotkeyItem>();

            // 현재 단축키 설정을 복사
            foreach (var shortcut in _settingsService.Settings.Shortcuts)
            {
                string displayName = _shortcutNames.ContainsKey(shortcut.Key) ? _shortcutNames[shortcut.Key] : shortcut.Key;
                _hotkeyItems.Add(new HotkeyItem(displayName, shortcut.Key, shortcut.Value));
            }

            HotkeysList.ItemsSource = _hotkeyItems;
            SaveLocationTextBox.Text = _settingsService.Settings.SaveDirectory;
            QuietModeCheckBox.IsChecked = _settingsService.Settings.QuietMode;
            AutoStartCheckBox.IsChecked = _settingsService.Settings.AutoStart;

            // 현재 자동 실행 상태 확인
            using (var key = Registry.CurrentUser.OpenSubKey(StartupRegistryKey))
            {
                if (key != null)
                {
                    var value = key.GetValue(AppName);
                    AutoStartCheckBox.IsChecked = value != null;
                }
            }
        }

        private void BrowseFolderButton_Click(object sender, RoutedEventArgs e)
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = "저장 경로 선택",
                UseDescriptionForTitle = true
            };

            if (!string.IsNullOrEmpty(_settingsService.Settings.SaveDirectory))
            {
                dialog.InitialDirectory = _settingsService.Settings.SaveDirectory;
            }

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                SaveLocationTextBox.Text = dialog.SelectedPath;
            }
        }

        private void ShortcutTextBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            e.Handled = true;

            var textBox = (System.Windows.Controls.TextBox)sender;

            // ESC 키가 눌리면 단축키 변경 모드를 취소
            if (e.Key == Key.Escape)
            {
                textBox.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                return;
            }

            // Ignore standalone modifier keys
            if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl ||
                e.Key == Key.LeftAlt || e.Key == Key.RightAlt ||
                e.Key == Key.LeftShift || e.Key == Key.RightShift ||
                e.Key == Key.System)
            {
                return;
            }

            var shortcutName = textBox.Tag?.ToString();
            if (string.IsNullOrEmpty(shortcutName)) return;

            var hotkeyItem = _hotkeyItems.First(x => x.CommandName == shortcutName);

            var modifiers = ModifierKeys.None;
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                modifiers |= ModifierKeys.Control;
            if (Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt))
                modifiers |= ModifierKeys.Alt;
            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                modifiers |= ModifierKeys.Shift;

            // Update the shortcut
            hotkeyItem.Shortcut = new KeyboardShortcut(e.Key, modifiers);
            hotkeyItem.UpdateShortcutText();

            // 단축키 입력이 완료되면 다음 컨트롤로 포커스 이동
            textBox.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
        }

        private void UpdateAutoStart(bool enable)
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(StartupRegistryKey, true))
                {
                    if (key == null)
                    {
                        MessageBox.Show("레지스트리 키에 접근할 수 없습니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    if (enable)
                    {
                        string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
                        key.SetValue(AppName, exePath);
                    }
                    else
                    {
                        key.DeleteValue(AppName, false);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"자동 실행 설정 중 오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ResetAllButton_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(
                "모든 설정을 기본값으로 초기화하시겠습니까?",
                "설정 초기화",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                // 임시로 설정을 초기화하여 기본값을 가져옴
                var tempSettings = new AppSettings();

                // 모든 단축키를 기본값으로 재설정
                foreach (var item in _hotkeyItems)
                {
                    if (tempSettings.Shortcuts.TryGetValue(item.CommandName, out var defaultShortcut))
                    {
                        item.Shortcut = defaultShortcut.Clone();
                        item.UpdateShortcutText();
                    }
                }

                // 저장 경로를 기본값으로 재설정
                SaveLocationTextBox.Text = tempSettings.SaveDirectory;
                QuietModeCheckBox.IsChecked = tempSettings.QuietMode;
                AutoStartCheckBox.IsChecked = tempSettings.AutoStart;
                UpdateAutoStart(tempSettings.AutoStart);
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Update settings with new shortcuts
                foreach (var item in _hotkeyItems)
                {
                    _settingsService.Settings.Shortcuts[item.CommandName] = item.Shortcut;
                }

                // Update save location and other settings
                _settingsService.Settings.SaveDirectory = SaveLocationTextBox.Text;
                _settingsService.Settings.QuietMode = QuietModeCheckBox.IsChecked ?? false;
                _settingsService.Settings.AutoStart = AutoStartCheckBox.IsChecked ?? true;

                // Update auto start registry
                UpdateAutoStart(AutoStartCheckBox.IsChecked ?? true);

                _settingsService.SaveSettings();
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"설정 저장 중 오류가 발생했습니다: {ex.Message}", 
                              "오류", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                DialogResult = false;
                Close();
            }
        }
    }

    public class HotkeyItem : INotifyPropertyChanged
    {
        public string Name { get; }
        public string CommandName { get; }
        public KeyboardShortcut Shortcut { get; set; }
        public bool IsGlobal { get; }
        private string _shortcutText = string.Empty;

        public string ShortcutText
        {
            get => _shortcutText;
            private set
            {
                _shortcutText = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShortcutText)));
            }
        }

        public HotkeyItem(string name, string commandName, KeyboardShortcut shortcut)
        {
            Name = name;
            CommandName = commandName;
            Shortcut = shortcut;
            // 화면 캡처 관련 단축키와 저장폴더 열기는 글로벌 단축키로 설정
            IsGlobal = commandName is "RegionSelect" or "FullScreen" or "WindowCapture" or "OpenSaveFolder";
            UpdateShortcutText();
        }

        public void UpdateShortcutText()
        {
            ShortcutText = Shortcut.ToString();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
} 