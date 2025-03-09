using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;

namespace MyCap.Services
{
    public class ImageSaveService
    {
        public void SaveImage(BitmapSource source, string path)
        {
            var extension = Path.GetExtension(path).ToLower();
            var format = GetImageFormat(extension);

            using var bitmap = ConvertToBitmap(source);
            bitmap.Save(path, format);
        }

        private ImageFormat GetImageFormat(string extension)
        {
            return extension switch
            {
                ".png" => ImageFormat.Png,
                ".jpg" or ".jpeg" => ImageFormat.Jpeg,
                ".bmp" => ImageFormat.Bmp,
                _ => throw new ArgumentException($"Unsupported image format: {extension}")
            };
        }

        private Bitmap ConvertToBitmap(BitmapSource source)
        {
            var width = source.PixelWidth;
            var height = source.PixelHeight;
            var stride = width * 4;
            var pixels = new byte[height * stride];

            source.CopyPixels(pixels, stride, 0);

            var bitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            var bitmapData = bitmap.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.WriteOnly,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            System.Runtime.InteropServices.Marshal.Copy(
                pixels, 0, bitmapData.Scan0, pixels.Length);

            bitmap.UnlockBits(bitmapData);
            return bitmap;
        }

        public string GenerateFileName(string baseDirectory)
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var path = Path.Combine(baseDirectory, $"capture_{timestamp}.png");
            var counter = 1;

            while (File.Exists(path))
            {
                path = Path.Combine(baseDirectory, $"capture_{timestamp}_{counter}.png");
                counter++;
            }

            return path;
        }
    }
} 