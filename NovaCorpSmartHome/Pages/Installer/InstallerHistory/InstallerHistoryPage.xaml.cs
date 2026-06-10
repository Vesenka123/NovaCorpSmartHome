using NovaCorpSmartHome.ApplicationData;
using NovaCorpSmartHome.Dialogs; // Подключаем диалоги
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace NovaCorpSmartHome.Pages.Installer.InstallerHistory
{
    /// <summary>
    /// Логика взаимодействия для InstallerHistoryPage.xaml
    /// </summary>
    public partial class InstallerHistoryPage : Page, INotifyPropertyChanged
    {
        private List<Installations> _allHistory;
        private List<Installations> _filteredHistory;
        private string _currentSortColumn = "CompletionDate";
        private ListSortDirection _currentSortDirection = ListSortDirection.Descending;

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public InstallerHistoryPage()
        {
            InitializeComponent();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadHistoryAsync();
        }

        // Асинхронная загрузка истории
        private async Task LoadHistoryAsync()
        {
            try
            {
                ShowLoading(true);

                await Task.Run(() =>
                {
                    int currentInstallerId = AppConnect.CurrentEmployee.Id;

                    // КРИТИЧНО ВАЖНО: Подгружаем ВСЕ связанные данные, включая товары и комментарии
                    _allHistory = AppConnect.modelOdb.Installations
                        .Include("Orders")                  // Заказ
                        .Include("Orders.Customers")        // Клиент
                        .Include("Orders.OrderItems")       // Позиции заказа
                        .Include("Orders.OrderItems.Products") // Товары
                        .Where(i => i.InstallerId == currentInstallerId && i.Status == "Завершена")
                        .OrderByDescending(i => i.CompletionDate)
                        .ToList();
                });

                _filteredHistory = new List<Installations>(_allHistory);
                ApplySorting();
                UpdateStatistics();
                ShowLoading(false);

                if (_filteredHistory == null || _filteredHistory.Count == 0)
                {
                    ShowEmpty(true);
                }
                else
                {
                    ShowEmpty(false);
                }
            }
            catch (Exception ex)
            {
                ShowLoading(false);
                MessageBox.Show($"Ошибка загрузки истории: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplySorting()
        {
            if (_filteredHistory == null || _filteredHistory.Count == 0)
            {
                dgHistory.ItemsSource = _filteredHistory;
                return;
            }

            IEnumerable<Installations> sorted = _filteredHistory;

            switch (_currentSortColumn)
            {
                case "Orders.Id":
                    sorted = _currentSortDirection == ListSortDirection.Ascending
                        ? _filteredHistory.OrderBy(i => i.Orders.Id)
                        : _filteredHistory.OrderByDescending(i => i.Orders.Id);
                    break;
                case "Orders.Customers.LastName":
                    sorted = _currentSortDirection == ListSortDirection.Ascending
                        ? _filteredHistory.OrderBy(i => i.Orders?.Customers?.LastName)
                        : _filteredHistory.OrderByDescending(i => i.Orders?.Customers?.LastName);
                    break;
                case "PlanDate":
                    sorted = _currentSortDirection == ListSortDirection.Ascending
                        ? _filteredHistory.OrderBy(i => i.PlanDate)
                        : _filteredHistory.OrderByDescending(i => i.PlanDate);
                    break;
                case "StartDate":
                    sorted = _currentSortDirection == ListSortDirection.Ascending
                        ? _filteredHistory.OrderBy(i => i.StartDate)
                        : _filteredHistory.OrderByDescending(i => i.StartDate);
                    break;
                case "CompletionDate":
                    sorted = _currentSortDirection == ListSortDirection.Ascending
                        ? _filteredHistory.OrderBy(i => i.CompletionDate)
                        : _filteredHistory.OrderByDescending(i => i.CompletionDate);
                    break;
                default:
                    sorted = _filteredHistory.OrderByDescending(i => i.CompletionDate);
                    break;
            }

            dgHistory.ItemsSource = sorted.ToList();
        }

        private void UpdateStatistics()
        {
            if (_filteredHistory == null) return;

            var total = _filteredHistory.Count;
            var currentYear = DateTime.Now.Year;
            var thisYearCount = _filteredHistory.Count(i => i.CompletionDate.HasValue && i.CompletionDate.Value.Year == currentYear);
            var lastMonthCount = _filteredHistory.Count(i => i.CompletionDate.HasValue && i.CompletionDate.Value.Month == DateTime.Now.AddMonths(-1).Month);

            txtStatistics.Text = $"📊 Всего выполнено: {total} | ✅ В этом году: {thisYearCount} | 📅 За прошлый месяц: {lastMonthCount}";
        }

        private void ShowLoading(bool show)
        {
            if (loadingPanel != null) loadingPanel.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            if (dgHistory != null) dgHistory.Visibility = show ? Visibility.Collapsed : Visibility.Visible;
        }

        private void ShowEmpty(bool show)
        {
            if (emptyPanel != null) emptyPanel.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            if (dgHistory != null && !show) dgHistory.Visibility = Visibility.Visible;
        }

        // --- НОВЫЙ МЕТОД: Открытие деталей завершенного заказа ---
        private void btnDetails_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var installation = button?.Tag as Installations;

            if (installation == null || installation.Orders == null)
            {
                MessageBox.Show("Ошибка: Данные заказа отсутствуют.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var order = installation.Orders;
            var customer = order.Customers;

            // 1. Формируем список товаров
            var itemsList = new System.Collections.ObjectModel.ObservableCollection<string>();
            if (order.OrderItems != null && order.OrderItems.Any())
            {
                foreach (var item in order.OrderItems)
                {
                    if (item.Products != null)
                    {
                        itemsList.Add($"{item.Products.Name} (Кол-во: {item.Quantity})");
                    }
                }
            }

            // 2. Получаем комментарий менеджера
            // ВНИМАНИЕ: Проверьте имя свойства в вашей модели EF! 
            // Обычно это Notes, Comment или Description.
            string managerCommentText = "Нет комментариев";

            // Попытка получить комментарий из разных возможных полей (защита от ошибок именования)
            try
            {
                // Если у вас поле называется Notes:
                if (order.Notes != null) managerCommentText = order.Notes;
                // Если у вас поле называется Comment:
                // else if (order.Comment != null) managerCommentText = order.Comment;
            }
            catch { /* Игнорируем ошибки доступа к свойствам */ }

            // 3. Получаем данные об установке (комментарий установщика и файл)
            string installerCommentText = "Нет отчета";
            string filePathText = "";

            try
            {
                // Комментарий установщика
                if (!string.IsNullOrEmpty(installation.InstallerNotes))
                {
                    installerCommentText = installation.InstallerNotes;
                }

                // Путь к файлу
                // ВНИМАНИЕ: Проверьте имя свойства! Оно может быть AttachmentPath, FilePath, FileLink и т.д.
                // Посмотрите в классе Installations в папке Models/ApplicationData, как точно называется свойство.
                if (installation.GetType().GetProperty("AttachmentPath") != null)
                {
                    // Используем рефлексию для безопасности, если свойство есть
                    var propVal = installation.GetType().GetProperty("AttachmentPath").GetValue(installation);
                    if (propVal != null) filePathText = propVal.ToString();
                }
                else if (installation.GetType().GetProperty("FilePath") != null)
                {
                    var propVal = installation.GetType().GetProperty("FilePath").GetValue(installation);
                    if (propVal != null) filePathText = propVal.ToString();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка получения данных установки: {ex.Message}");
            }

            // 4. Открываем диалог
            var dialog = new OrderDetailsDialog
            {
                OrderId = order.Id,
                ClientName = $"{customer.LastName} {customer.FirstName}",
                ClientPhone = customer.Phone ?? "Не указан",
                ClientAddress = customer.Address ?? "Не указан",
                OrderItems = itemsList,
                ManagerComment = managerCommentText,

                // ИЗМЕНЕНО: Вместо true ставим Visibility.Visible
                IsInstallationInfoVisible = Visibility.Visible,

                InstallerComment = installerCommentText,
                FilePath = filePathText
            };

            dialog.ShowDialog();
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService?.CanGoBack == true)
                NavigationService.GoBack();
            else
                NavigationService?.Navigate(new InstallerDashboardPage());
        }

        private void dgHistory_Sorting(object sender, DataGridSortingEventArgs e)
        {
            var column = e.Column;

            if (_currentSortColumn == column.SortMemberPath)
            {
                _currentSortDirection = _currentSortDirection == ListSortDirection.Ascending
                    ? ListSortDirection.Descending
                    : ListSortDirection.Ascending;
            }
            else
            {
                _currentSortColumn = column.SortMemberPath;
                _currentSortDirection = ListSortDirection.Ascending;
            }

            foreach (var col in dgHistory.Columns)
            {
                col.SortDirection = null;
            }
            column.SortDirection = _currentSortDirection;

            e.Handled = true;
            ApplySorting();
        }
    }
}