using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Drawing;
using System.Windows.Forms;
using MessageBox = System.Windows.MessageBox;
using MyCap.Services;
using MyCap.Windows;
using System.Linq;

namespace MyCap
{
    public partial class MainWindow : Window
    {
        private BitmapSource? currentCapture;
        private string? lastSavePath;
        private readonly ScreenCaptureService captureService;
        private readonly ImageSaveService saveService;
        private readonly SettingsService settingsService;

        public MainWindow()
        {
            InitializeComponent();
            InitializeCommands();
            captureService = new ScreenCaptureService();
            saveService = new ImageSaveService();
            settingsService = new SettingsService();

            // Initialize button click handlers
            var fullScreenButton = FindName("FullScreenButton") as System.Windows.Controls.Button;
            var regionSelectButton = FindName("RegionSelectButton") as System.Windows.Controls.Button;
            var windowCaptureButton = FindName("WindowCaptureButton") as System.Windows.Controls.Button;
            var saveAsButton = FindName("SaveAsButton") as System.Windows.Controls.Button;
            var copyButton = FindName("CopyButton") as System.Windows.Controls.Button;

            if (fullScreenButton != null) fullScreenButton.Click += (s, e) => CaptureFullScreen();
            if (regionSelectButton != null) regionSelectButton.Click += (s, e) => CaptureRegion();
            if (windowCaptureButton != null) windowCaptureButton.Click += (s, e) => CaptureWindow();
            if (saveAsButton != null) saveAsButton.Click += (s, e) => ExecuteSaveAs(s, null);
            if (copyButton != null) copyButton.Click += (s, e) => ExecuteCopy(s, null);

            // Initialize AutoSave menu item
            var autoSaveMenuItem = FindName("AutoSaveMenuItem") as System.Windows.Controls.MenuItem;
            if (autoSaveMenuItem != null)
            {
                autoSaveMenuItem.IsChecked = settingsService.Settings.AutoSave;
                settingsService.Settings.AutoSave = true; // Set default to true
                settingsService.SaveSettings();
            }
        }

        private void InitializeCommands()
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

            // Add hotkeys for capture modes
            var regionSelectCommand = new RoutedCommand("RegionSelect", typeof(MainWindow));
            CommandBindings.Add(new CommandBinding(regionSelectCommand, (s, e) => CaptureRegion()));
            var regionSelectGesture = new KeyGesture(Key.F1, ModifierKeys.Control);
            InputBindings.Add(new KeyBinding(regionSelectCommand, regionSelectGesture));

            var fullScreenCommand = new RoutedCommand("FullScreen", typeof(MainWindow));
            CommandBindings.Add(new CommandBinding(fullScreenCommand, (s, e) => CaptureFullScreen()));
            var fullScreenGesture = new KeyGesture(Key.F2, ModifierKeys.Control);
            InputBindings.Add(new KeyBinding(fullScreenCommand, fullScreenGesture));

            var saveAsCommand = new RoutedCommand("SaveAs", typeof(MainWindow));
            CommandBindings.Add(new CommandBinding(saveAsCommand, ExecuteSaveAs));
            var saveAsMenuItem = FindName("SaveAsMenuItem") as System.Windows.Controls.MenuItem;
            if (saveAsMenuItem != null) saveAsMenuItem.Command = saveAsCommand;

            var windowCaptureCommand = new RoutedCommand("WindowCapture", typeof(MainWindow));
            CommandBindings.Add(new CommandBinding(windowCaptureCommand, (s, e) => CaptureWindow()));
            var windowCaptureMenuItem = FindName("WindowCaptureMenuItem") as System.Windows.Controls.MenuItem;
            if (windowCaptureMenuItem != null) windowCaptureMenuItem.Command = windowCaptureCommand;

            // Add hotkey bindings
            var saveAsInputBinding = new KeyBinding(saveAsCommand, Key.S, ModifierKeys.Control | ModifierKeys.Shift);
            InputBindings.Add(saveAsInputBinding);

            var windowCaptureInputBinding = new KeyBinding(windowCaptureCommand, Key.F3, ModifierKeys.Control);
            InputBindings.Add(windowCaptureInputBinding);

            // Add F1 key binding for About window
            var aboutCommand = new RoutedCommand("About", typeof(MainWindow));
            CommandBindings.Add(new CommandBinding(aboutCommand, (s, e) => ShowAboutWindow()));
            var aboutGesture = new KeyGesture(Key.F1);
            InputBindings.Add(new KeyBinding(aboutCommand, aboutGesture));

            // Add ESC key binding for closing dialogs
            var closeDialogCommand = new RoutedCommand("CloseDialog", typeof(MainWindow));
            CommandBindings.Add(new CommandBinding(closeDialogCommand, (s, e) => CloseActiveDialog()));
            var escGesture = new KeyGesture(Key.Escape);
            InputBindings.Add(new KeyBinding(closeDialogCommand, escGesture));

            // Add hotkey to button
            var saveAsButton = FindName("SaveAsButton") as System.Windows.Controls.Button;
            if (saveAsButton != null) saveAsButton.Command = saveAsCommand;

            var windowCaptureButton = FindName("WindowCaptureButton") as System.Windows.Controls.Button;
            if (windowCaptureButton != null) windowCaptureButton.Command = windowCaptureCommand;
        }

        private void ExecuteNewCapture(object sender, ExecutedRoutedEventArgs? e)
        {
            var captureMode = MessageBox.Show(
                "Select capture mode:\n\n" +
                "Yes - Full Screen\n" +
                "No - Region Select\n" +
                "Cancel - Window Capture",
                "Capture Mode",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question);

            switch (captureMode)
            {
                case MessageBoxResult.Yes:
                    CaptureFullScreen();
                    break;
                case MessageBoxResult.No:
                    CaptureRegion();
                    break;
                case MessageBoxResult.Cancel:
                    CaptureWindow();
                    break;
            }
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

        private void CaptureFullScreen()
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
                    currentCapture = capture;
                    PreviewImage.Source = capture;
                    UpdateImageSizeInfo(capture);

                    // Show save dialog if auto-save is disabled
                    if (!settingsService.Settings.AutoSave)
                    {
                        // ExecuteSaveAs(null, null);
                    }
                    else
                    {
                        // Auto-save the capture
                        AutoSaveCapture();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error capturing screen: {ex.Message}", "Capture Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
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

        private void CaptureRegion()
        {
            try
            {
                // Hide the main window temporarily
                this.Hide();
                System.Threading.Thread.Sleep(300); // Give more time for the window to hide

                var regionWindow = new RegionSelectWindow();
                regionWindow.RegionSelected += (s, capture) =>
                {
                    currentCapture = capture;
                    PreviewImage.Source = currentCapture;
                    UpdateImageSizeInfo(capture);
                    AutoSaveCapture();
                };
                regionWindow.Closed += (s, e) =>
                {
                    // Show the main window again when region selection is closed
                    this.Show();
                    this.WindowState = WindowState.Normal;
                    this.Activate();
                    this.Focus();
                    this.Topmost = true;
                    this.Topmost = false;
                };
                regionWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error capturing region: {ex.Message}", "Capture Error", MessageBoxButton.OK, MessageBoxImage.Error);
                this.Show();
                this.Activate();
            }
        }

        private void CaptureWindow()
        {
            try
            {
                // Hide the main window temporarily
                this.Hide();
                System.Threading.Thread.Sleep(300); // Give more time for the window to hide

                var windowSelectWindow = new WindowSelectWindow();
                windowSelectWindow.WindowSelected += (s, capture) =>
                {
                    currentCapture = capture;
                    PreviewImage.Source = currentCapture;
                    UpdateImageSizeInfo(capture);
                    AutoSaveCapture();
                };
                windowSelectWindow.Closed += (s, e) =>
                {
                    // Show the main window again when window selection is closed
                    this.Show();
                    this.WindowState = WindowState.Normal;
                    this.Activate();
                    this.Focus();
                    this.Topmost = true;
                    this.Topmost = false;
                };
                windowSelectWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error capturing window: {ex.Message}", "Capture Error", MessageBoxButton.OK, MessageBoxImage.Error);
                this.Show();
                this.Activate();
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
            var settingsWindow = new SettingsWindow(settingsService);
            settingsWindow.Owner = this;
            settingsWindow.ShowDialog();
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
            Close();
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
            var hotkeysWindow = new HotkeysWindow();
            hotkeysWindow.Owner = this;
            hotkeysWindow.ShowDialog();
        }

        private void SaveLocationMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new FolderBrowserDialog
            {
                Description = "Select Save Location",
                UseDescriptionForTitle = true,
                SelectedPath = settingsService.Settings.SaveDirectory
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                settingsService.Settings.SaveDirectory = dialog.SelectedPath;
                settingsService.SaveSettings();
            }
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
    }
} 