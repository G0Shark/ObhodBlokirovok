using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ProcessViewer
{
    public partial class ProcessLIst : Window
    {
        public ObservableCollection<ProcessItem> Processes { get; set; } = new();

        public ProcessLIst()
        {
            InitializeComponent();
            LoadProcesses();
            ProcessList.ItemsSource = Processes;
        }

        private void LoadProcesses()
        {
            Processes.Clear();

            foreach (var process in Process.GetProcesses())
            {
                try
                {
                    string path = process.MainModule?.FileName ?? "";
                    ImageSource? icon = null;

                    if (File.Exists(path))
                        icon = GetIconFromFile(path);

                    Processes.Add(new ProcessItem
                    {
                        Name = process.ProcessName,
                        Id = process.Id,
                        Icon = icon
                    });
                }
                catch
                {
                    // Игнорируем процессы, к которым нет доступа
                    continue;
                }
            }
        }

        private ImageSource? GetIconFromFile(string fileName)
        {
            IntPtr hIcon = ExtractIcon(IntPtr.Zero, fileName, 0);
            if (hIcon == IntPtr.Zero)
                return null;

            try
            {
                ImageSource img = Imaging.CreateBitmapSourceFromHIcon(
                    hIcon,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
                return img;
            }
            finally
            {
                DestroyIcon(hIcon);
            }
        }

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr ExtractIcon(IntPtr hInst, string lpszExeFileName, int nIconIndex);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DestroyIcon(IntPtr hIcon);
    }

    public class ProcessItem
    {
        public string Name { get; set; } = "";
        public int Id { get; set; }
        public ImageSource? Icon { get; set; }
    }
}
