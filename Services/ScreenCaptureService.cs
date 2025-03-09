using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Media.Imaging;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows;

namespace MyCap.Services
{
    public class ScreenCaptureService
    {
        public BitmapSource CaptureFullScreen()
        {
            using var bitmap = new Bitmap(
                Screen.PrimaryScreen!.Bounds.Width,
                Screen.PrimaryScreen.Bounds.Height,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.CopyFromScreen(0, 0, 0, 0, bitmap.Size);
            }

            return ConvertToBitmapSource(bitmap);
        }

        public BitmapSource CaptureRegion(Rectangle region)
        {
            using var bitmap = new Bitmap(
                region.Width,
                region.Height,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.CopyFromScreen(region.X, region.Y, 0, 0, region.Size);
            }

            return ConvertToBitmapSource(bitmap);
        }

        public BitmapSource CaptureWindow(IntPtr windowHandle)
        {
            var window = new WindowWrapper(windowHandle);
            var bounds = window.Bounds;

            using var bitmap = new Bitmap(
                bounds.Width,
                bounds.Height,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.CopyFromScreen(bounds.X, bounds.Y, 0, 0, bounds.Size);
            }

            return ConvertToBitmapSource(bitmap);
        }

        private BitmapSource ConvertToBitmapSource(Bitmap bitmap)
        {
            var handle = bitmap.GetHbitmap();
            try
            {
                return Imaging.CreateBitmapSourceFromHBitmap(
                    handle,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
            }
            finally
            {
                DeleteObject(handle);
            }
        }

        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        private class WindowWrapper : System.Windows.Forms.IWin32Window
        {
            public WindowWrapper(IntPtr handle)
            {
                Handle = handle;
            }

            public IntPtr Handle { get; }

            public Rectangle Bounds
            {
                get
                {
                    if (GetWindowRect(Handle, out RECT rect))
                    {
                        return new Rectangle(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
                    }
                    return Rectangle.Empty;
                }
            }

            [System.Runtime.InteropServices.DllImport("user32.dll")]
            private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

            [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
            private struct RECT
            {
                public int Left;
                public int Top;
                public int Right;
                public int Bottom;
            }
        }
    }
} 