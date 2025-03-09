using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MyCap.Windows
{
    public partial class HotkeysWindow : Window
    {
        public HotkeysWindow()
        {
            InitializeComponent();
            LoadHotkeys();
        }

        private void LoadHotkeys()
        {
            var hotkeysList = new[]
            {
                new { Action = "About", Hotkey = "F1" },
                new { Action = "Region Select", Hotkey = "Ctrl+F1" },
                new { Action = "Full Screen", Hotkey = "Ctrl+F2" },
                new { Action = "Window Capture", Hotkey = "Ctrl+F3" },
                new { Action = "New Capture", Hotkey = "Ctrl+N" },
                new { Action = "Save", Hotkey = "Ctrl+S" },
                new { Action = "Save As", Hotkey = "Ctrl+Shift+S" },
                new { Action = "Copy", Hotkey = "Ctrl+C" },
                new { Action = "Close Dialog", Hotkey = "Esc" },
                new { Action = "Exit", Hotkey = "Alt+F4" }
            };

            var listView = FindName("HotkeysListView") as System.Windows.Controls.ListView;
            if (listView != null)
            {
                listView.ItemsSource = hotkeysList;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
        }
    }
} 