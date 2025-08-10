using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Drawing;
using System.Windows.Forms;
using MessageBox = System.Windows.MessageBox;
using Application = System.Windows.Application;
using MyCap.Services;
using MyCap.Windows;
using System.Linq;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Windows.Interop;
using System.IO;
using System;

namespace MyCap
{
    public partial class MainWindow : Window
    {
        // Win32 API for registering global hotkeys
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        // Win32 API for screen flash effect
        [DllImport("user32.dll")]
        private static extern bool FlashWindow(IntPtr hWnd, bool bInvert);

        [DllImport("user32.dll")]
        private static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool InvalidateRect(IntPtr hWnd, IntPtr lpRect, bool bErase);

        [DllImport("user32.dll")]
        private static extern bool UpdateWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateSolidBrush(uint crColor);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        [DllImport("user32.dll")]
        private static extern bool FillRect(IntPtr hDC, ref RECT lprc, IntPtr hbr);

        [DllImport("user32.dll")]
        private static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseWindowDC(IntPtr hWnd, IntPtr hDC);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        // Modifier keys
        private const uint MOD_ALT = 0x0001;
        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_SHIFT = 0x0004;
        private const uint MOD_WIN = 0x0008;
        private const uint MOD_NOREPEAT = 0x4000;

        // Dictionary to store registered hotkey IDs
        private readonly Dictionary<string, int> _registeredHotkeys = new();
        private int _hotkeyId = 1;

        private BitmapSource? currentCapture;
        private string? lastSavePath;
        private readonly ScreenCaptureService captureService;
        private readonly ImageSaveService saveService;
        private readonly SettingsService settingsService;
        
        // 동일 영역 캡처를 위한 저장된 영역 좌표 (설정에서 로드됨)
        private System.Drawing.Rectangle? savedRegion;
        
        // NotifyIcon for system tray functionality
        private NotifyIcon? notifyIcon;

        public MainWindow()
        {
            try
            {
                if (!App.CreatedNew)
                {
                    // If this is not the first instance, close the window
                    Close();
                    return;
                }

                InitializeComponent();
                
                // Initialize services
                try
                {
                    captureService = new ScreenCaptureService();
                    saveService = new ImageSaveService();
                    settingsService = new SettingsService();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error initializing services: {ex.Message}");
                    MessageBox.Show("서비스 초기화 중 오류가 발생했습니다.", "초기화 오류", MessageBoxButton.OK, MessageBoxImage.Error);
                    Close();
                    return;
                }

                // Set up Window events
                this.Loaded += MainWindow_Loaded;
                this.SourceInitialized += MainWindow_SourceInitialized;

                // Initialize button click handlers
                var fullScreenButton = FindName("FullScreenButton") as System.Windows.Controls.Button;
                var regionSelectButton = FindName("RegionSelectButton") as System.Windows.Controls.Button;
                var windowCaptureButton = FindName("WindowCaptureButton") as System.Windows.Controls.Button;
                var saveAsButton = FindName("SaveAsButton") as System.Windows.Controls.Button;
                var copyButton = FindName("CopyButton") as System.Windows.Controls.Button;

                if (fullScreenButton != null) fullScreenButton.Click += (s, e) => CaptureFullScreen(false);
                if (regionSelectButton != null) regionSelectButton.Click += (s, e) => CaptureRegion(false);
                if (windowCaptureButton != null) windowCaptureButton.Click += (s, e) => CaptureWindow(false);
                if (saveAsButton != null) saveAsButton.Click += (s, e) => ExecuteSaveAs(s, null);
                if (copyButton != null) copyButton.Click += (s, e) => ExecuteCopy(s, null);

                // 저장된 영역 로드
                LoadSavedRegion();

                // Initialize AutoSave menu item
                var autoSaveMenuItem = FindName("AutoSaveMenuItem") as System.Windows.Controls.MenuItem;
                if (autoSaveMenuItem != null && settingsService != null)
                {
                    autoSaveMenuItem.IsChecked = settingsService.Settings.AutoSave;
                }

                // Initialize system tray icon
                InitializeNotifyIcon();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in MainWindow constructor: {ex.Message}");
                MessageBox.Show("프로그램 초기화 중 오류가 발생했습니다.", "초기화 오류", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }

        private void MainWindow_SourceInitialized(object sender, EventArgs e)
        {
            // Window handle is now available, initialize commands and register global hotkeys
            var source = PresentationSource.FromVisual(this) as HwndSource;
            source?.AddHook(WndProc);

            // Initialize commands after window handle is available
            InitializeCommands();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;
            if (msg == WM_HOTKEY)
            {
                try
                {
                    int hotkeyId = wParam.ToInt32();
                    foreach (var kvp in _registeredHotkeys)
                    {
                        if (kvp.Value == hotkeyId)
                        {
                            System.Diagnostics.Debug.WriteLine($"Global hotkey triggered: {kvp.Key}");
                            
                            // 메인 UI 스레드에서 실행
                            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                            {
                                try
                                {
                                    switch (kvp.Key)
                                    {
                                        case "RegionSelect":
                                            CaptureRegion(true);
                                            break;
                                        case "FullScreen":
                                            CaptureFullScreen(true);
                                            break;
                                        case "WindowCapture":
                                            CaptureWindow(true);
                                            break;
                                                        case "OpenSaveFolder":
                    OpenSavedFolderButton_Click(null, null);
                    break;
                case "SameRegionCapture":
                    CaptureSameRegion(true);
                    break;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine($"Error executing global hotkey action: {ex.Message}");
                                    MessageBox.Show(
                                        $"단축키 동작 실행 중 오류가 발생했습니다: {ex.Message}",
                                        "단축키 실행 오류",
                                        MessageBoxButton.OK,
                                        MessageBoxImage.Error);
                                }
                            }));

                            handled = true;
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error processing global hotkey: {ex.Message}");
                }
            }
            return IntPtr.Zero;
        }

        private void RegisterGlobalHotkey(string name, KeyboardShortcut shortcut)
        {
            try
            {
                if (_registeredHotkeys.ContainsKey(name))
                {
                    UnregisterHotKey(new WindowInteropHelper(this).Handle, _registeredHotkeys[name]);
                    _registeredHotkeys.Remove(name);
                }

                uint modifiers = 0;
                if ((shortcut.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt)
                    modifiers |= MOD_ALT;
                if ((shortcut.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                    modifiers |= MOD_CONTROL;
                if ((shortcut.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                    modifiers |= MOD_SHIFT;
                modifiers |= MOD_NOREPEAT;

                int id = _hotkeyId++;
                var hwnd = new WindowInteropHelper(this).Handle;
                
                // Try to register the hotkey
                if (!RegisterHotKey(hwnd, id, modifiers, (uint)KeyInterop.VirtualKeyFromKey(shortcut.Key)))
                {
                    // If registration fails, try without MOD_NOREPEAT
                    modifiers &= ~MOD_NOREPEAT;
                    if (!RegisterHotKey(hwnd, id, modifiers, (uint)KeyInterop.VirtualKeyFromKey(shortcut.Key)))
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to register global hotkey for {name}");
                        MessageBox.Show(
                            $"'{name}' 단축키를 글로벌 단축키로 등록하는데 실패했습니다.\n" +
                            "다른 프로그램에서 이미 사용 중일 수 있습니다.",
                            "단축키 등록 실패",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                        return;
                    }
                }

                _registeredHotkeys[name] = id;
                System.Diagnostics.Debug.WriteLine($"Successfully registered global hotkey for {name}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error registering global hotkey for {name}: {ex.Message}");
                MessageBox.Show(
                    $"글로벌 단축키 등록 중 오류가 발생했습니다: {ex.Message}",
                    "단축키 등록 오류",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void UnregisterAllHotkeys()
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            foreach (var id in _registeredHotkeys.Values)
            {
                UnregisterHotKey(hwnd, id);
            }
            _registeredHotkeys.Clear();
        }

        private void InitializeCommands()
        {
            if (settingsService == null)
            {
                System.Diagnostics.Debug.WriteLine("SettingsService is null, cannot initialize commands");
                MessageBox.Show("설정 서비스를 초기화할 수 없습니다.", "설정 오류", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                // File Menu Commands
                CommandBindings.Add(new CommandBinding(ApplicationCommands.New, ExecuteNewCapture));
                CommandBindings.Add(new CommandBinding(ApplicationCommands.Save, ExecuteSave));
                CommandBindings.Add(new CommandBinding(ApplicationCommands.SaveAs, ExecuteSaveAs));
                
                // Edit Menu Commands
                CommandBindings.Add(new CommandBinding(ApplicationCommands.Copy, ExecuteCopy));

                // Settings Command
                var settingsCommand = new RoutedCommand("Settings", typeof(MainWindow));
                CommandBindings.Add(new CommandBinding(settingsCommand, ExecuteSettings));

                // 단축키 설정 시 예외 처리를 위한 메서드
                void SetupShortcut(string name, Action action)
                {
                    try
                    {
                        var command = new RoutedCommand(name, typeof(MainWindow));
                        CommandBindings.Add(new CommandBinding(command, (s, e) => action()));
                        
                        if (settingsService.Settings.Shortcuts.TryGetValue(name, out var shortcut))
                        {
                            System.Diagnostics.Debug.WriteLine($"Setting up shortcut for {name}: {shortcut}");
                            // 글로벌 단축키인 경우 Win32 API를 사용하여 등록
                            if (name is "RegionSelect" or "FullScreen" or "WindowCapture" or "SameRegionCapture")
                            {
                                RegisterGlobalHotkey(name, shortcut);
                            }
                            else
                            {
                                InputBindings.Add(new KeyBinding(command, shortcut.Key, shortcut.Modifiers));
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"Shortcut not found for {name}, using default");
                            settingsService.ResetToDefaults();
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error setting up {name} shortcut: {ex.Message}");
                        // 오류 발생 시 기본값으로 재설정
                        settingsService.ResetToDefaults();
                    }
                }

                // 각 단축키 설정
                SetupShortcut("RegionSelect", () => CaptureRegion(true));
                SetupShortcut("FullScreen", () => CaptureFullScreen(true));
                SetupShortcut("WindowCapture", () => CaptureWindow(true));
                SetupShortcut("SameRegionCapture", () => CaptureSameRegion(true));

                // SaveAs 명령 설정
                var saveAsCommand = new RoutedCommand("SaveAs", typeof(MainWindow));
                CommandBindings.Add(new CommandBinding(saveAsCommand, ExecuteSaveAs));
                var saveAsMenuItem = FindName("SaveAsMenuItem") as System.Windows.Controls.MenuItem;
                if (saveAsMenuItem != null) saveAsMenuItem.Command = saveAsCommand;

                try
                {
                    if (settingsService.Settings.Shortcuts.TryGetValue("SaveAs", out var saveAsShortcut))
                    {
                        InputBindings.Add(new KeyBinding(saveAsCommand, saveAsShortcut.Key, saveAsShortcut.Modifiers));
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error setting up SaveAs shortcut: {ex.Message}");
                }

                // About 명령 설정
                SetupShortcut("About", ShowAboutWindow);

                // CloseDialog 명령 설정
                SetupShortcut("CloseDialog", CloseActiveDialog);

                // ExitApplication 명령 설정
                SetupShortcut("ExitApplication", CloseApplication);

                // 버튼에 명령 연결
                var saveAsButton = FindName("SaveAsButton") as System.Windows.Controls.Button;
                if (saveAsButton != null) saveAsButton.Command = saveAsCommand;

                var windowCaptureButton = FindName("WindowCaptureButton") as System.Windows.Controls.Button;
                if (windowCaptureButton != null && 
                    settingsService.Settings.Shortcuts.TryGetValue("WindowCapture", out var windowCaptureShortcut))
                {
                    var windowCaptureCommand = new RoutedCommand("WindowCapture", typeof(MainWindow));
                    CommandBindings.Add(new CommandBinding(windowCaptureCommand, (s, e) => CaptureWindow()));
                    windowCaptureButton.Command = windowCaptureCommand;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing commands: {ex.Message}");
                MessageBox.Show("단축키 설정 중 오류가 발생했습니다. 기본값으로 재설정됩니다.", 
                               "단축키 설정 오류", 
                               MessageBoxButton.OK, 
                               MessageBoxImage.Warning);
                try
                {
                    settingsService.ResetToDefaults();
                }
                catch (Exception resetEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Error resetting to defaults: {resetEx.Message}");
                }
            }
        }

        // 실행시, 처음에는 트레이 아이콘으로 보이지 않고, UI창으로 보인다.
        private void InitializeNotifyIcon()
        {
            try
            {
                // Use embedded resource for icon to avoid path issues
                using (var iconStream = System.Reflection.Assembly.GetExecutingAssembly()
                    .GetManifestResourceStream("MyCap.Resources.mycap.ico"))
                {
                    if (iconStream != null)
                    {
                        notifyIcon = new NotifyIcon
                        {
                            Icon = new Icon(iconStream),
                            Visible = false,
                            Text = "MyCap 화면 캡처"
                        };
                    }
                    else
                    {
                        // Fallback to system icon if resource not found
                        notifyIcon = new NotifyIcon
                        {
                            Icon = System.Drawing.SystemIcons.Application,
                            Visible = false,
                            Text = "MyCap 화면 캡처"
                        };
                        
                        System.Diagnostics.Debug.WriteLine("Warning: Could not load icon from resources, using system icon instead.");
                    }
                }
            }
            catch (Exception ex)
            {
                // Fallback to system icon if there's any error
                notifyIcon = new NotifyIcon
                {
                    Icon = System.Drawing.SystemIcons.Application,
                    Visible = false,
                    Text = "MyCap 화면 캡처"
                };
                
                System.Diagnostics.Debug.WriteLine($"Error loading icon: {ex.Message}");
            }
            
            // Double-click to show window
            notifyIcon.DoubleClick += (s, e) => ShowMainWindow();
            
            // Create context menu
            var contextMenu = new ContextMenuStrip();
            
            // Add "Open" menu item
            var openMenuItem = new ToolStripMenuItem("열기");
            openMenuItem.Click += (s, e) => ShowMainWindow();
            contextMenu.Items.Add(openMenuItem);
            
            // Add separator
            contextMenu.Items.Add(new ToolStripSeparator());
            
            // Add "Exit" menu item
            var exitMenuItem = new ToolStripMenuItem("종료");
            exitMenuItem.Click += (s, e) => CloseApplication();
            contextMenu.Items.Add(exitMenuItem);
            
            // Assign context menu to notify icon
            notifyIcon.ContextMenuStrip = contextMenu;
        }
        
        private void ShowMainWindow()
        {
            // Show and activate the window
            Show();
            WindowState = WindowState.Normal;
            Activate();
        }
        
        private void EnsureTrayIconVisible()
        {
            if (notifyIcon != null)
            {
                try
                {
                    // Ensure the tray icon is visible
                    notifyIcon.Visible = true;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error showing tray icon: {ex.Message}");
                }
            }
        }

        private void FlashScreen()
        {
            try
            {
                // Create a full-screen white window for flash effect
                var flashWindow = new Window
                {
                    WindowStyle = WindowStyle.None,
                    AllowsTransparency = true,
                    Background = System.Windows.Media.Brushes.White,
                    Topmost = true,
                    ShowInTaskbar = false,
                    ResizeMode = ResizeMode.NoResize,
                    WindowState = WindowState.Maximized,
                    Left = 0,
                    Top = 0,
                    Width = SystemParameters.VirtualScreenWidth,
                    Height = SystemParameters.VirtualScreenHeight
                };

                // Show the flash window
                flashWindow.Show();
                
                // Schedule to close the flash window after a short delay
                var timer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(150) // 150ms white flash
                };
                timer.Tick += (s, e) =>
                {
                    try
                    {
                        flashWindow.Close();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error closing flash window: {ex.Message}");
                    }
                    finally
                    {
                        timer.Stop();
                    }
                };
                timer.Start();
                
                System.Diagnostics.Debug.WriteLine("White screen flash effect triggered");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error flashing screen: {ex.Message}");
            }
        }
        
        private void CloseApplication()
        {
            // 컨텍스트 메뉴에서 명시적 종료. 
            // Clean up notify icon before closing
            if (notifyIcon != null)
            {
                notifyIcon.Visible = false;
                notifyIcon.Dispose();
                notifyIcon = null;
            }
            
            // Exit application
            System.Windows.Application.Current.Shutdown();
        }
        
        // Override OnClosing to minimize to tray instead of closing
        protected override void OnClosing(CancelEventArgs e)
        {
            // X버튼으로 종료시, 트레이로 가게 한다. 
               
            // Cancel the close
            e.Cancel = true;
            
            // Hide the window
            Hide();
            
            // Show the notify icon if it's not already visible
            if (notifyIcon != null)
            {
                try
                {
                    // Ensure the icon is visible
                    notifyIcon.Visible = true;
                    
                    // Show a balloon tip to inform the user
                    notifyIcon.ShowBalloonTip(
                        200, 
                        "MyCap", 
                        "프로그램이 시스템 트레이로 최소화되었습니다.", 
                        ToolTipIcon.Info);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error showing tray icon: {ex.Message}");
                }
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Initialize the notify icon but keep it hidden until needed
            // We don't need to do anything here as the icon is already initialized
            // and set to not visible in the InitializeNotifyIcon method
        }

        // Method to clean up resources when application is closing
        protected override void OnClosed(EventArgs e)
        {
            UnregisterAllHotkeys();
            base.OnClosed(e);
        }

        private void ExecuteNewCapture(object sender, ExecutedRoutedEventArgs? e)
        {
            CaptureRegion(false);
        }

        private void HandleNewCapture(BitmapSource capture, bool isHotkeyTriggered = false)
        {
            currentCapture = capture;
            UpdateImageSizeInfo(currentCapture);
            System.Windows.Clipboard.SetImage(currentCapture);

            if (settingsService.Settings.AutoSave)
            {
                var fileName = $"Screenshot_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.{settingsService.Settings.DefaultFormat}";
                lastSavePath = Path.Combine(settingsService.Settings.SaveDirectory, fileName);
                SaveCapture(lastSavePath);

                if (!isHotkeyTriggered || !settingsService.Settings.QuietMode)
                {
                    Show();
                    WindowState = WindowState.Normal;
                    if (PreviewImage != null)
                    {
                        PreviewImage.Source = currentCapture;
                    }
                }
                else
                {
                    // In Quiet Mode, ensure tray icon is visible and flash screen to confirm capture
                    EnsureTrayIconVisible();
                    FlashScreen();
                }
            }
            else if (!isHotkeyTriggered || !settingsService.Settings.QuietMode)
            {
                Show();
                WindowState = WindowState.Normal;
                if (PreviewImage != null)
                {
                    PreviewImage.Source = currentCapture;
                }
            }
            else
            {
                // In Quiet Mode, ensure tray icon is visible and flash screen to confirm capture
                EnsureTrayIconVisible();
                FlashScreen();
            }
        }

        private void ExecuteFullScreenCapture(object sender, ExecutedRoutedEventArgs? e)
        {
            CaptureFullScreen(false);
        }

        private void ExecuteWindowCapture(object sender, ExecutedRoutedEventArgs? e)
        {
            CaptureWindow(false);
        }

        private void UpdateImageSizeInfo(BitmapSource? image)
        {
            var imageSizeTextBlock = FindName("ImageSizeTextBlock") as System.Windows.Controls.TextBlock;
            if (imageSizeTextBlock != null)
            {
                if (image != null)
                {
                    imageSizeTextBlock.Text = $"Image Size: {image.PixelWidth} x {image.PixelHeight} pixels";
                }
                else
                {
                    imageSizeTextBlock.Text = "No image captured";
                }
            }
        }

        private void CaptureFullScreen(bool isHotkeyTriggered = false)
        {
            try
            {
                // Hide main window before capture
                this.Hide();
                System.Threading.Thread.Sleep(300); // Give more time for the window to hide

                // Capture full screen
                var capture = captureService.CaptureFullScreen();
                if (capture != null)
                {
                    HandleNewCapture(capture, isHotkeyTriggered);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error capturing screen: {ex.Message}", "Capture Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (!isHotkeyTriggered || !settingsService.Settings.QuietMode)
                {
                    // Show main window after capture
                    this.Show();
                    this.WindowState = WindowState.Normal;
                    this.Activate();
                    this.Focus();
                    this.Topmost = true;
                    this.Topmost = false;
                }
            }
        }

        private void CaptureRegion(bool isHotkeyTriggered = false)
        {
            try
            {
                // Hide the main window temporarily
                this.Hide();
                System.Threading.Thread.Sleep(300); // Give more time for the window to hide

                var regionWindow = new RegionSelectWindow();
                regionWindow.RegionSelected += (s, capture) =>
                {
                    if (capture != null)
                    {
                        HandleNewCapture(capture, isHotkeyTriggered);
                    }
                };
                regionWindow.RegionCoordinatesSelected += (s, rect) =>
                {
                    savedRegion = rect;
                    SaveRegionToSettings(rect);
                };
                regionWindow.Closed += (s, e) =>
                {
                    if (!isHotkeyTriggered || !settingsService.Settings.QuietMode)
                    {
                        // Show the main window again when region selection is closed
                        this.Show();
                        this.WindowState = WindowState.Normal;
                        this.Activate();
                        this.Focus();
                        this.Topmost = true;
                        this.Topmost = false;
                    }
                };
                regionWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error capturing region: {ex.Message}", "Capture Error", MessageBoxButton.OK, MessageBoxImage.Error);
                if (!isHotkeyTriggered || !settingsService.Settings.QuietMode)
                {
                    this.Show();
                    this.Activate();
                }
            }
        }

        private void CaptureWindow(bool isHotkeyTriggered = false)
        {
            try
            {
                // Hide the main window temporarily
                this.Hide();
                System.Threading.Thread.Sleep(300); // Give more time for the window to hide

                var windowSelectWindow = new WindowSelectWindow();
                windowSelectWindow.WindowSelected += (s, capture) =>
                {
                    if (capture != null)
                    {
                        HandleNewCapture(capture, isHotkeyTriggered);
                    }
                };
                windowSelectWindow.Closed += (s, e) =>
                {
                    if (!isHotkeyTriggered || !settingsService.Settings.QuietMode)
                    {
                        // Show the main window again when window selection is closed
                        this.Show();
                        this.WindowState = WindowState.Normal;
                        this.Activate();
                        this.Focus();
                        this.Topmost = true;
                        this.Topmost = false;
                    }
                };
                windowSelectWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error capturing window: {ex.Message}", "Capture Error", MessageBoxButton.OK, MessageBoxImage.Error);
                if (!isHotkeyTriggered || !settingsService.Settings.QuietMode)
                {
                    this.Show();
                    this.Activate();
                }
            }
        }

        private void CaptureSameRegion(bool isHotkeyTriggered = false)
        {
            try
            {
                if (!savedRegion.HasValue)
                {
                    MessageBox.Show("저장된 영역이 없습니다. 먼저 영역 캡처를 수행하여 영역을 설정해주세요.", 
                                   "동일 영역 캡처", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                this.Hide();
                System.Threading.Thread.Sleep(100);

                var capture = captureService.CaptureRegion(savedRegion.Value);
                if (capture != null)
                {
                    HandleNewCapture(capture, isHotkeyTriggered);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"동일 영역 캡처 중 오류가 발생했습니다: {ex.Message}", "캡처 오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (!isHotkeyTriggered || !settingsService.Settings.QuietMode)
                {
                    this.Show();
                }
            }
        }

        private void LoadSavedRegion()
        {
            try
            {
                savedRegion = settingsService.Settings.GetSavedRegion();
                if (savedRegion.HasValue)
                {
                    System.Diagnostics.Debug.WriteLine($"저장된 영역 로드됨: {savedRegion.Value}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"저장된 영역 로드 중 오류: {ex.Message}");
            }
        }

        private void SaveRegionToSettings(System.Drawing.Rectangle region)
        {
            try
            {
                settingsService.Settings.SetSavedRegion(region);
                settingsService.SaveSettings();
                System.Diagnostics.Debug.WriteLine($"영역 저장됨: {region}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"영역 저장 중 오류: {ex.Message}");
            }
        }

        private void AutoSaveCapture()
        {
            if (settingsService.Settings.AutoSave && currentCapture != null)
            {
                try
                {
                    var path = saveService.GenerateFileName(settingsService.Settings.SaveDirectory);
                    saveService.SaveImage(currentCapture, path);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error auto-saving capture: {ex.Message}", "Auto-Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ExecuteSave(object sender, ExecutedRoutedEventArgs? e)
        {
            if (currentCapture == null)
            {
                MessageBox.Show("No capture to save.", "Save Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(lastSavePath))
            {
                ExecuteSaveAs(sender, e);
            }
            else
            {
                SaveCapture(lastSavePath);
            }
        }

        private void ExecuteSaveAs(object sender, ExecutedRoutedEventArgs? e)
        {
            var dialog = new SaveFileDialog
            {
                Filter = "PNG Image|*.png|JPEG Image|*.jpg|BMP Image|*.bmp",
                Title = "Save Capture As",
                DefaultExt = "png",
                InitialDirectory = settingsService.Settings.SaveDirectory
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                lastSavePath = dialog.FileName;
                SaveCapture(lastSavePath);
            }
        }

        private void SaveCapture(string path)
        {
            try
            {
                saveService.SaveImage(currentCapture!, path);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving capture: {ex.Message}", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteCopy(object sender, ExecutedRoutedEventArgs? e)
        {
            if (currentCapture != null)
            {
                System.Windows.Clipboard.SetImage(currentCapture);
            }
        }

        private void ExecuteSettings(object sender, ExecutedRoutedEventArgs e)
        {
            // 설정 창을 열기 전에 글로벌 단축키 해제
            UnregisterAllHotkeys();

            var settingsWindow = new SettingsWindow(settingsService);
            settingsWindow.Owner = this;
            if (settingsWindow.ShowDialog() == true)
            {
                // 설정이 변경되었을 때만 명령을 다시 초기화
                InitializeCommands();
            }
            else
            {
                // 설정이 취소되었을 때는 기존 단축키를 다시 등록
                RegisterExistingHotkeys();
            }
        }

        private void RegisterExistingHotkeys()
        {
            foreach (var shortcut in settingsService.Settings.Shortcuts)
            {
                if (shortcut.Key is "RegionSelect" or "FullScreen" or "WindowCapture" or "SameRegionCapture" or "OpenSaveFolder")
                {
                    RegisterGlobalHotkey(shortcut.Key, shortcut.Value);
                }
            }
        }

        private void ShowAboutWindow()
        {
            var aboutWindow = new AboutWindow();
            aboutWindow.Owner = this;
            aboutWindow.ShowDialog();
        }

        private void CloseActiveDialog()
        {
            var activeWindow = System.Windows.Application.Current.Windows.OfType<Window>()
                .FirstOrDefault(w => w != this && w.IsActive);
            if (activeWindow != null)
            {
                activeWindow.Close();
            }
        }

        // Menu Item Click Event Handlers
        private void NewCaptureMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ExecuteNewCapture(sender, null);
        }

        private void SaveMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ExecuteSave(sender, null);
        }

        private void SaveAsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ExecuteSaveAs(sender, null);
        }

        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            CloseApplication();
        }

        private void CopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ExecuteCopy(sender, null);
        }

        private void PreviewMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (currentCapture != null)
            {
                var previewWindow = new PreviewWindow(currentCapture);
                previewWindow.Owner = this;
                previewWindow.ShowDialog();
            }
            else
            {
                MessageBox.Show("No capture to preview.", "Preview Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void HistoryMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var historyWindow = new HistoryWindow();
            historyWindow.Owner = this;
            historyWindow.ShowDialog();
        }

        private void HotkeysMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // 설정 창을 열기 전에 글로벌 단축키 해제
            UnregisterAllHotkeys();

            var settingsWindow = new SettingsWindow(settingsService);
            settingsWindow.Owner = this;
            if (settingsWindow.ShowDialog() == true)
            {
                // 설정이 변경되었을 때만 명령을 다시 초기화
                InitializeCommands();
            }
            else
            {
                // 설정이 취소되었을 때는 기존 단축키를 다시 등록
                RegisterExistingHotkeys();
            }
        }

        private void SaveLocationMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow(settingsService);
            settingsWindow.Owner = this;
            settingsWindow.ShowDialog();
        }

        private void AutoSaveMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as System.Windows.Controls.MenuItem;
            if (menuItem != null)
            {
                settingsService.Settings.AutoSave = menuItem.IsChecked;
                settingsService.SaveSettings();
            }
        }

        private void AboutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var aboutWindow = new AboutWindow();
            aboutWindow.Owner = this;
            aboutWindow.ShowDialog();
        }

        // Method to minimize the application to the system tray
        private void MinimizeToTrayMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // Hide the window
            Hide();
            
            // Show the notify icon if it's not already visible
            if (notifyIcon != null)
            {
                try
                {
                    // Ensure the icon is visible
                    notifyIcon.Visible = true;
                    
                    // Show a balloon tip to inform the user
                    notifyIcon.ShowBalloonTip(
                        200, 
                        "MyCap", 
                        "프로그램이 시스템 트레이로 최소화되었습니다.", 
                        ToolTipIcon.Info);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error showing tray icon: {ex.Message}");
                    
                    // If we can't show the tray icon, show the window again
                    Show();
                    WindowState = WindowState.Normal;
                    Activate();
                    
                    MessageBox.Show(
                        "시스템 트레이로 최소화하는 데 문제가 발생했습니다.\n" + ex.Message,
                        "오류",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }

        private void OpenSavedFolderButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveDirectory = settingsService.Settings.SaveDirectory;
                if (string.IsNullOrEmpty(saveDirectory) || !System.IO.Directory.Exists(saveDirectory))
                {
                    MessageBox.Show(
                        "저장 폴더가 설정되어 있지 않거나 존재하지 않습니다.\n설정에서 저장 폴더를 확인해주세요.",
                        "폴더 열기 오류",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                // Windows Explorer로 폴더 열기
                System.Diagnostics.Process.Start("explorer.exe", saveDirectory);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error opening save directory: {ex.Message}");
                MessageBox.Show(
                    $"저장 폴더를 여는 중 오류가 발생했습니다: {ex.Message}",
                    "폴더 열기 오류",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
} 