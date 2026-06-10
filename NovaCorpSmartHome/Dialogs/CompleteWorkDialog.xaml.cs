using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Media;

namespace NovaCorpSmartHome.Dialogs
{
    public partial class CompleteWorkDialog : Window
    {
        public string Comment { get; private set; }
        public string FilePath { get; private set; } // Путь к файлу на диске

        public CompleteWorkDialog()
        {
            InitializeComponent();
        }

        private void btnSelectFile_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Все файлы|*.*|PDF|*.pdf|Изображения|*.jpg;*.jpeg;*.png;*.bmp|Документы|*.doc;*.docx",
                Title = "Выберите файл акта или фото"
            };

            if (dialog.ShowDialog() == true)
            {
                FilePath = dialog.FileName;
                txtFilePath.Text = Path.GetFileName(FilePath);
                txtFilePath.Foreground = Brushes.Green;
            }
        }

        private void btnConfirm_Click(object sender, RoutedEventArgs e)
        {
            Comment = txtComment.Text;
            DialogResult = true;
            Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}