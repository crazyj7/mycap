using System;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using MessageBox = System.Windows.MessageBox;

namespace MyCap.Windows
{
    public partial class HistoryWindow : Window
    {
        private readonly string saveDirectory;

        public HistoryWindow()
        {
            InitializeComponent();
            saveDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
                "MyCap"
            );
            LoadHistory();
        }

        private void LoadHistory()
        {
            if (!Directory.Exists(saveDirectory))
            {
                return;
            }

            var files = Directory.GetFiles(saveDirectory, "*.*")
                               .Where(f => f.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                                         f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                                         f.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase))
                               .OrderByDescending(f => File.GetCreationTime(f));

            foreach (var file in files)
            {
                var fileInfo = new FileInfo(file);
                var item = new
                {
                    Date = fileInfo.CreationTime,
                    FileName = fileInfo.Name,
                    Size = FormatFileSize(fileInfo.Length),
                    Type = fileInfo.Extension.ToUpper()
                };
                HistoryListView.Items.Add(item);
            }
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            int order = 0;
            double size = bytes;
            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }
            return $"{size:0.##} {sizes[order]}";
        }

        private void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = HistoryListView.SelectedItem;
            if (selectedItem == null)
            {
                MessageBox.Show("Please select a file to open.", "Open Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var fileName = ((dynamic)selectedItem).FileName;
            var filePath = Path.Combine(saveDirectory, fileName);

            if (File.Exists(filePath))
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true
                });
            }
            else
            {
                MessageBox.Show("File not found.", "Open Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
} 