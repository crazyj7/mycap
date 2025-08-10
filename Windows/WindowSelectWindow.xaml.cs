using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Forms;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Diagnostics;
using MyCap.Services;

namespace MyCap.Windows
{
    public partial class WindowSelectWindow : Window
    {
        private readonly ScreenCaptureService captureService;
        private IntPtr selectedWindowHandle;

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool ClientToScreen(IntPtr hWnd, out POINT lpPoint);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        private const int SW_RESTORE = 9;
        private const int SW_SHOW = 5;

        public event EventHandler<BitmapSource>? WindowSelected;
        public event EventHandler<System.Drawing.Rectangle>? WindowBoundsSelected;

        public WindowSelectWindow()
        {
            InitializeComponent();
            captureService = new ScreenCaptureService();
            LoadWindows();

            // Add double click event handler for WindowList
            if (WindowList is System.Windows.Controls.ListBox listBox)
            {
                listBox.MouseDoubleClick += WindowList_MouseDoubleClick;
            }
        }

        private void LoadWindows()
        {
            var windows = Process.GetProcesses()
                .Where(p => !string.IsNullOrEmpty(p.MainWindowTitle))
                .Select(p => new WindowInfo(p.MainWindowHandle))
                .OrderBy(w => w.Title)
                .ToList();

            WindowList.ItemsSource = windows;
        }

        private void WindowList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (WindowList.SelectedItem is WindowInfo window)
            {
                selectedWindowHandle = window.Handle;
            }
        }

        private void WindowList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (WindowList.SelectedItem is WindowInfo window)
            {
                selectedWindowHandle = window.Handle;
                CaptureButton_Click(sender, e);
            }
        }

        private void CaptureButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedWindowHandle != IntPtr.Zero)
            {
                try
                {
                    // Hide the window selection window first
                    this.Hide();
                    System.Threading.Thread.Sleep(100); // Give time for the window to hide

                    // Bring the selected window to foreground
                    ShowWindow(selectedWindowHandle, SW_RESTORE);
                    SetForegroundWindow(selectedWindowHandle);
                    System.Threading.Thread.Sleep(200); // Give more time for the window to come to foreground

                    // Get window bounds
                    var window = new WindowInfo(selectedWindowHandle);
                    var capture = captureService.CaptureRegion(window.Bounds);
                    WindowSelected?.Invoke(this, capture);
                    WindowBoundsSelected?.Invoke(this, window.Bounds);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Error capturing window: {ex.Message}", "Capture Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    Close();
                }
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
            base.OnKeyDown(e);
        }

        private class WindowInfo
        {
            public string Title { get; set; } = string.Empty;
            public string ProcessName { get; set; } = string.Empty;
            public IntPtr Handle { get; set; }

            public WindowInfo(IntPtr handle)
            {
                Handle = handle;
                if (handle != IntPtr.Zero)
                {
                    var process = Process.GetProcesses().FirstOrDefault(p => p.MainWindowHandle == handle);
                    if (process != null)
                    {
                        Title = process.MainWindowTitle;
                        ProcessName = process.ProcessName;
                    }
                }
            }

            public Rectangle Bounds
            {
                get
                {
                    if (Handle == IntPtr.Zero)
                        return Rectangle.Empty;

                    // Get client area
                    RECT clientRect;
                    if (!GetClientRect(Handle, out clientRect))
                        return Rectangle.Empty;

                    // Get client area position on screen
                    POINT clientPoint;
                    if (!ClientToScreen(Handle, out clientPoint))
                        return Rectangle.Empty;

                    return new Rectangle(
                        clientPoint.X,
                        clientPoint.Y,
                        clientRect.Right - clientRect.Left,
                        clientRect.Bottom - clientRect.Top
                    );
                }
            }
        }
    }
} 