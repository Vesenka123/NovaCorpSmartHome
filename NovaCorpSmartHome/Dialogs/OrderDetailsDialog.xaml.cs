using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace NovaCorpSmartHome.Dialogs
{
    public partial class OrderDetailsDialog : Window
    {
        public int OrderId { get; set; }
        public string ClientName { get; set; }
        public string ClientPhone { get; set; }
        public string ClientAddress { get; set; }
        public ObservableCollection<string> OrderItems { get; set; } = new ObservableCollection<string>();
        public string ManagerComment { get; set; }

        // Новые свойства для истории установок
        public Visibility IsInstallationInfoVisible { get; set; } = Visibility.Collapsed;
        public string InstallerComment { get; set; }
        public string FilePath { get; set; }

        // Вычисляемое свойство для видимости кнопки "Открыть файл"
        public Visibility HasFile => !string.IsNullOrEmpty(FilePath) ? Visibility.Visible : Visibility.Collapsed;

        // Имя файла для отображения
        public string FileName => !string.IsNullOrEmpty(FilePath) ? Path.GetFileName(FilePath) : "Нет файла";

        public OrderDetailsDialog()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void btnOpenFile_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(FilePath))
            {
                try
                {
                    // Если путь относительный (как мы сохраняли /Attachments/...), делаем его абсолютным
                    string absolutePath = FilePath;
                    if (FilePath.StartsWith("/"))
                    {
                        absolutePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, FilePath.TrimStart('/'));
                    }

                    if (File.Exists(absolutePath))
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = absolutePath,
                            UseShellExecute = true
                        });
                    }
                    else
                    {
                        MessageBox.Show("Файл не найден по указанному пути.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Не удалось открыть файл: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}