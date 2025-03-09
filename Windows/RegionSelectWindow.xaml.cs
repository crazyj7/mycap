using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Drawing;
using MyCap.Services;

namespace MyCap.Windows
{
    public partial class RegionSelectWindow : Window
    {
        private readonly ScreenCaptureService captureService;
        private System.Windows.Point startPoint;
        private System.Windows.Shapes.Rectangle selectionRectangle;
        private bool isSelecting;
        private BitmapSource? originalImage;
        private System.Windows.Controls.Image? previewImage;

        public event EventHandler<BitmapSource>? RegionSelected;

        public RegionSelectWindow()
        {
            InitializeComponent();
            captureService = new ScreenCaptureService();
            InitializeCapture();
        }

        private void InitializeCapture()
        {
            try
            {
                // Capture the full screen
                originalImage = captureService.CaptureFullScreen();
                if (originalImage != null)
                {
                    // Create a background image
                    var backgroundImage = new System.Windows.Controls.Image
                    {
                        Source = originalImage,
                        Stretch = Stretch.None,
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                        VerticalAlignment = System.Windows.VerticalAlignment.Top
                    };
                    Canvas.SetLeft(backgroundImage, 0);
                    Canvas.SetTop(backgroundImage, 0);
                    MainCanvas.Children.Add(backgroundImage);

                    // Create a dark overlay
                    var overlay = new System.Windows.Shapes.Rectangle
                    {
                        Fill = new SolidColorBrush(System.Windows.Media.Color.FromArgb(128, 0, 0, 0)),
                        Width = SystemParameters.PrimaryScreenWidth,
                        Height = SystemParameters.PrimaryScreenHeight
                    };
                    Canvas.SetLeft(overlay, 0);
                    Canvas.SetTop(overlay, 0);
                    MainCanvas.Children.Add(overlay);

                    // Create a selection rectangle
                    selectionRectangle = new System.Windows.Shapes.Rectangle
                    {
                        Stroke = new SolidColorBrush(Colors.White),
                        StrokeThickness = 2,
                        Fill = new SolidColorBrush(Colors.Transparent),
                        Visibility = Visibility.Collapsed
                    };
                    MainCanvas.Children.Add(selectionRectangle);

                    // Create a preview image for the selection area
                    previewImage = new System.Windows.Controls.Image
                    {
                        Stretch = Stretch.None,
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                        VerticalAlignment = System.Windows.VerticalAlignment.Top,
                        Visibility = Visibility.Collapsed
                    };
                    MainCanvas.Children.Add(previewImage);

                    // Set window properties
                    WindowStyle = WindowStyle.None;
                    ResizeMode = ResizeMode.NoResize;
                    Topmost = true;
                    WindowState = WindowState.Maximized;
                    Background = System.Windows.Media.Brushes.Transparent;
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error initializing capture: {ex.Message}", "Capture Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }

        private void MainCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                startPoint = e.GetPosition(MainCanvas);
                selectionRectangle.Width = 0;
                selectionRectangle.Height = 0;
                selectionRectangle.Visibility = Visibility.Visible;
                Canvas.SetLeft(selectionRectangle, startPoint.X);
                Canvas.SetTop(selectionRectangle, startPoint.Y);
                isSelecting = true;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error starting selection: {ex.Message}", "Selection Error", MessageBoxButton.OK, MessageBoxImage.Error);
                isSelecting = false;
            }
        }

        private void MainCanvas_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (isSelecting)
            {
                try
                {
                    var currentPoint = e.GetPosition(MainCanvas);
                    var width = currentPoint.X - startPoint.X;
                    var height = currentPoint.Y - startPoint.Y;

                    selectionRectangle.Width = Math.Abs(width);
                    selectionRectangle.Height = Math.Abs(height);

                    Canvas.SetLeft(selectionRectangle, width > 0 ? startPoint.X : currentPoint.X);
                    Canvas.SetTop(selectionRectangle, height > 0 ? startPoint.Y : currentPoint.Y);

                    // Update preview image
                    if (originalImage != null && previewImage != null)
                    {
                        var rect = new Int32Rect(
                            (int)Canvas.GetLeft(selectionRectangle),
                            (int)Canvas.GetTop(selectionRectangle),
                            (int)selectionRectangle.Width,
                            (int)selectionRectangle.Height
                        );

                        if (rect.Width > 0 && rect.Height > 0)
                        {
                            var croppedBitmap = new CroppedBitmap(originalImage, rect);
                            previewImage.Source = croppedBitmap;
                            previewImage.Visibility = Visibility.Visible;
                            Canvas.SetLeft(previewImage, Canvas.GetLeft(selectionRectangle));
                            Canvas.SetTop(previewImage, Canvas.GetTop(selectionRectangle));
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Error updating selection: {ex.Message}", "Selection Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    isSelecting = false;
                }
            }
        }

        private void MainCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            isSelecting = false;
            if (selectionRectangle.Width > 0 && selectionRectangle.Height > 0)
            {
                CaptureSelectedRegion();
            }
            else
            {
                selectionRectangle.Visibility = Visibility.Collapsed;
                if (previewImage != null)
                {
                    previewImage.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void CaptureSelectedRegion()
        {
            try
            {
                var rect = new System.Drawing.Rectangle(
                    (int)Canvas.GetLeft(selectionRectangle),
                    (int)Canvas.GetTop(selectionRectangle),
                    (int)selectionRectangle.Width,
                    (int)selectionRectangle.Height
                );

                var capture = captureService.CaptureRegion(rect);
                RegionSelected?.Invoke(this, capture);
                Close();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error capturing region: {ex.Message}", "Capture Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
            base.OnKeyDown(e);
        }
    }
} 