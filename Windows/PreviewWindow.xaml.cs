using System.Windows;
using System.Windows.Media.Imaging;

namespace MyCap.Windows
{
    public partial class PreviewWindow : Window
    {
        public PreviewWindow(BitmapSource image)
        {
            InitializeComponent();
            PreviewImage.Source = image;
        }
    }
} 