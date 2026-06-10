using Microsoft.Win32;
using NovaCorpSmartHome.ApplicationData;
using NovaCorpSmartHome.Pages.Admin.AdminDashboard;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace NovaCorpSmartHome.Pages.Admin.AdminInstallations
{
    public partial class AdminInstallationsPage : Page, INotifyPropertyChanged
    {
        // Модели данных
        public class InstallationView
        {
            public int OrderId { get; set; }
            public string ClientName { get; set; }
            public string Address { get; set; }
            public string InstallerName { get; set; }
            public DateTime? PlanDate { get; set; }
            public DateTime? StartDate { get; set; }
            public DateTime? CompletionDate { get; set; }
            public string Status { get; set; }
            public int InstallerId { get; set; }
            public int InstallationId { get; set; }
        }

        public class InstallerItem
        {
            public int Id { get; set; }
            public string Name { get; set; }

            public override string ToString()
            {
                return Name;
            }
        }

        // Поля класса
        private List<InstallationView> _allInstallations;
        private List<InstallationView> _filteredInstallations;
        private string _currentSortColumn = "PlanDate";
        private ListSortDirection _currentSortDirection = ListSortDirection.Descending;
        private string _currentQuickFilter = "All"; // All, Today, Week, Month

        // Свойства для MVVM
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        // Конструктор
        public AdminInstallationsPage()
        {
            InitializeComponent();
        }

        // События загрузки страницы
        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadDataAsync();
        }

        // Асинхронная загрузка данных
        private async Task LoadDataAsync()
        {
            try
            {
                ShowLoading(true);

                var result = await Task.Run(() =>
                {
                    // Загрузка установщиков
                    var installers = AppConnect.modelOdb.Employees
                        .Where(e => e.Role == "Установщик")
                        .ToList()
                        .Select(e => new InstallerItem
                        {
                            Id = e.Id,
                            Name = $"{e.LastName} {e.FirstName}".Trim()
                        })
                        .OrderBy(i => i.Name)
                        .ToList();

                    // Загрузка установок
                    var installations = AppConnect.modelOdb.Installations
                        .Include("Orders")
                        .Include("Orders.Customers")
                        .Include("Employees")
                        .OrderByDescending(i => i.PlanDate)
                        .ToList();

                    // Преобразование в плоскую модель
                    var allInstallations = installations.Select(i => new InstallationView
                    {
                        OrderId = i.OrderId,
                        ClientName = i.Orders?.Customers != null
                            ? $"{i.Orders.Customers.LastName} {i.Orders.Customers.FirstName}".Trim()
                            : "Не указан",
                        Address = i.Orders?.Customers?.Address ?? "Нет адреса",
                        InstallerName = i.Employees != null
                            ? $"{i.Employees.LastName} {i.Employees.FirstName}".Trim()
                            : "Не назначен",
                        PlanDate = i.PlanDate,
                        StartDate = i.StartDate,
                        CompletionDate = i.CompletionDate,
                        Status = i.Status ?? "Неизвестно",
                        InstallerId = i.InstallerId,
                        InstallationId = i.Id
                    }).ToList();

                    return new { installers, allInstallations };
                });

                // Обновление UI
                cmbInstallerFilter.Items.Clear();
                cmbInstallerFilter.Items.Add(new InstallerItem { Id = 0, Name = "Все установщики" });
                foreach (var inst in result.installers)
                    cmbInstallerFilter.Items.Add(inst);
                cmbInstallerFilter.SelectedIndex = 0;

                _allInstallations = result.allInstallations;
                _filteredInstallations = new List<InstallationView>(_allInstallations);

                ApplyFilters();
                ShowLoading(false);
            }
            catch (Exception ex)
            {
                ShowLoading(false);
                MessageBox.Show($"Ошибка загрузки данных:\n{ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Применение всех фильтров
        private void ApplyFilters()
        {
            if (_allInstallations == null) return;

            var filtered = _allInstallations.AsEnumerable();

            // Фильтр по установщику
            var selectedInstaller = cmbInstallerFilter.SelectedItem as InstallerItem;
            if (selectedInstaller != null && selectedInstaller.Id != 0)
            {
                filtered = filtered.Where(i => i.InstallerId == selectedInstaller.Id);
            }

            // Фильтр по статусу
            var selectedStatus = (cmbStatusFilter.SelectedItem as ComboBoxItem)?.Content?.ToString();
            if (selectedStatus != null && selectedStatus != "Все статусы")
            {
                filtered = filtered.Where(i => i.Status == selectedStatus);
            }

            // Фильтр по дате (быстрые фильтры)
            if (_currentQuickFilter == "Today")
            {
                var today = DateTime.Today;
                filtered = filtered.Where(i => i.PlanDate.HasValue && i.PlanDate.Value.Date == today);
            }
            else if (_currentQuickFilter == "Week")
            {
                var startOfWeek = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek + (int)DayOfWeek.Monday);
                var endOfWeek = startOfWeek.AddDays(6);
                filtered = filtered.Where(i => i.PlanDate.HasValue && i.PlanDate.Value.Date >= startOfWeek && i.PlanDate.Value.Date <= endOfWeek);
            }
            else if (_currentQuickFilter == "Month")
            {
                var startOfMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);
                filtered = filtered.Where(i => i.PlanDate.HasValue && i.PlanDate.Value.Date >= startOfMonth && i.PlanDate.Value.Date <= endOfMonth);
            }

            // Поиск по клиенту или адресу
            if (!string.IsNullOrWhiteSpace(txtSearch.Text))
            {
                var searchTerm = txtSearch.Text.ToLower();
                filtered = filtered.Where(i =>
                    i.ClientName.ToLower().Contains(searchTerm) ||
                    i.Address.ToLower().Contains(searchTerm));
            }

            _filteredInstallations = filtered.ToList();
            ApplySorting();
            UpdateStatistics();
        }

        // Применение сортировки
        private void ApplySorting()
        {
            if (_filteredInstallations == null || _filteredInstallations.Count == 0)
            {
                dgInstallations.ItemsSource = _filteredInstallations;
                return;
            }

            IEnumerable<InstallationView> sorted = _filteredInstallations;

            switch (_currentSortColumn)
            {
                case "OrderId":
                    sorted = _currentSortDirection == ListSortDirection.Ascending
                        ? _filteredInstallations.OrderBy(i => i.OrderId)
                        : _filteredInstallations.OrderByDescending(i => i.OrderId);
                    break;
                case "ClientName":
                    sorted = _currentSortDirection == ListSortDirection.Ascending
                        ? _filteredInstallations.OrderBy(i => i.ClientName)
                        : _filteredInstallations.OrderByDescending(i => i.ClientName);
                    break;
                case "InstallerName":
                    sorted = _currentSortDirection == ListSortDirection.Ascending
                        ? _filteredInstallations.OrderBy(i => i.InstallerName)
                        : _filteredInstallations.OrderByDescending(i => i.InstallerName);
                    break;
                case "PlanDate":
                    sorted = _currentSortDirection == ListSortDirection.Ascending
                        ? _filteredInstallations.OrderBy(i => i.PlanDate)
                        : _filteredInstallations.OrderByDescending(i => i.PlanDate);
                    break;
                case "StartDate":
                    sorted = _currentSortDirection == ListSortDirection.Ascending
                        ? _filteredInstallations.OrderBy(i => i.StartDate)
                        : _filteredInstallations.OrderByDescending(i => i.StartDate);
                    break;
                case "CompletionDate":
                    sorted = _currentSortDirection == ListSortDirection.Ascending
                        ? _filteredInstallations.OrderBy(i => i.CompletionDate)
                        : _filteredInstallations.OrderByDescending(i => i.CompletionDate);
                    break;
                case "Status":
                    sorted = _currentSortDirection == ListSortDirection.Ascending
                        ? _filteredInstallations.OrderBy(i => i.Status)
                        : _filteredInstallations.OrderByDescending(i => i.Status);
                    break;
                default:
                    sorted = _filteredInstallations.OrderByDescending(i => i.PlanDate);
                    break;
            }

            dgInstallations.ItemsSource = sorted.ToList();
        }

        // Обновление статистики
        private void UpdateStatistics()
        {
            if (_filteredInstallations == null) return;

            var total = _filteredInstallations.Count;
            var pending = _filteredInstallations.Count(i => i.Status == "Ожидает установки");
            var inProgress = _filteredInstallations.Count(i => i.Status == "В работе");
            var completed = _filteredInstallations.Count(i => i.Status == "Завершена");

            txtStatistics.Text = $"📊 Всего: {total} | ⏳ Ожидает: {pending} | 🔧 В работе: {inProgress} | ✅ Завершено: {completed}";
        }

        // Показать/скрыть индикатор загрузки
        private void ShowLoading(bool show)
        {
            if (loadingPanel != null)
            {
                loadingPanel.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            }

            if (dgInstallations != null)
            {
                dgInstallations.Visibility = show ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        // Экспорт в CSV
        private async void ExportToCsv()
        {
            try
            {
                if (_filteredInstallations == null || _filteredInstallations.Count == 0)
                {
                    MessageBox.Show("Нет данных для экспорта", "Информация",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "CSV файлы (*.csv)|*.csv",
                    DefaultExt = "csv",
                    FileName = $"Установки_{DateTime.Now:yyyyMMdd_HHmmss}"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    ShowLoading(true);

                    await Task.Run(() =>
                    {
                        var sb = new StringBuilder();
                        // Заголовки
                        sb.AppendLine("\"№ Заказа\";\"Клиент\";\"Адрес\";\"Установщик\";\"Плановая дата\";\"Начало работ\";\"Завершение\";\"Статус\"");

                        // Данные
                        foreach (var item in _filteredInstallations)
                        {
                            sb.AppendLine($"\"{item.OrderId}\";\"{item.ClientName}\";\"{item.Address}\";\"{item.InstallerName}\";" +
                                $"\"{item.PlanDate:dd.MM.yyyy}\";\"{item.StartDate:dd.MM.yyyy HH:mm}\";\"{item.CompletionDate:dd.MM.yyyy HH:mm}\";\"{item.Status}\"");
                        }

                        System.IO.File.WriteAllText(saveFileDialog.FileName, sb.ToString(), Encoding.UTF8);
                    });

                    ShowLoading(false);
                    MessageBox.Show($"Данные успешно экспортированы в файл:\n{saveFileDialog.FileName}",
                        "Экспорт завершен", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                ShowLoading(false);
                MessageBox.Show($"Ошибка при экспорте:\n{ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Просмотр деталей установки
        private void ViewDetails_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var installation = button?.Tag as InstallationView;

            if (installation != null)
            {
                var details = $"Детали установки #{installation.OrderId}\n\n" +
                    $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n" +
                    $"📋 Заказ №: {installation.OrderId}\n" +
                    $"👤 Клиент: {installation.ClientName}\n" +
                    $"📍 Адрес: {installation.Address}\n" +
                    $"🔧 Установщик: {installation.InstallerName}\n" +
                    $"📊 Статус: {installation.Status}\n" +
                    $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n" +
                    $"📅 Плановая дата: {installation.PlanDate:dd.MM.yyyy}\n" +
                    $"⏰ Начало работ: {installation.StartDate:dd.MM.yyyy HH:mm}\n" +
                    $"✅ Завершение: {installation.CompletionDate:dd.MM.yyyy HH:mm}";

                MessageBox.Show(details, "Информация об установке",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // Сброс кнопок быстрых фильтров
        private void ResetQuickFilterButtons()
        {
            var buttons = new[] { btnToday, btnWeek, btnMonth, btnAll };
            foreach (var btn in buttons)
            {
                if (btn != null)
                    btn.Style = (Style)FindResource("FilterButtonStyle");
            }
        }

        // Установка активной кнопки фильтра
        private void SetActiveFilterButton(Button activeButton)
        {
            if (activeButton != null)
                activeButton.Style = (Style)FindResource("ActiveFilterButtonStyle");
        }

        // ========== ОБРАБОТЧИКИ СОБЫТИЙ ==========

        // Фильтр по установщику
        private void cmbInstallerFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        // Фильтр по статусу
        private void cmbStatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        // Поиск
        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        // Сброс всех фильтров
        private void btnResetFilters_Click(object sender, RoutedEventArgs e)
        {
            // Сброс основных фильтров
            cmbInstallerFilter.SelectedIndex = 0;
            cmbStatusFilter.SelectedIndex = 0;
            txtSearch.Text = "";

            // Сброс быстрых фильтров
            _currentQuickFilter = "All";
            ResetQuickFilterButtons();
            SetActiveFilterButton(btnAll);

            // Применение фильтров
            ApplyFilters();
        }

        // Быстрый фильтр: Сегодня
        private void btnToday_Click(object sender, RoutedEventArgs e)
        {
            _currentQuickFilter = "Today";
            ResetQuickFilterButtons();
            SetActiveFilterButton(btnToday);
            ApplyFilters();
        }

        // Быстрый фильтр: Эта неделя
        private void btnWeek_Click(object sender, RoutedEventArgs e)
        {
            _currentQuickFilter = "Week";
            ResetQuickFilterButtons();
            SetActiveFilterButton(btnWeek);
            ApplyFilters();
        }

        // Быстрый фильтр: Этот месяц
        private void btnMonth_Click(object sender, RoutedEventArgs e)
        {
            _currentQuickFilter = "Month";
            ResetQuickFilterButtons();
            SetActiveFilterButton(btnMonth);
            ApplyFilters();
        }

        // Быстрый фильтр: Все
        private void btnAll_Click(object sender, RoutedEventArgs e)
        {
            _currentQuickFilter = "All";
            ResetQuickFilterButtons();
            SetActiveFilterButton(btnAll);
            ApplyFilters();
        }

        // Экспорт данных
        private void btnExport_Click(object sender, RoutedEventArgs e)
        {
            ExportToCsv();
        }

        // Кнопка назад
        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService?.CanGoBack == true)
                NavigationService.GoBack();
            else
                NavigationService?.Navigate(new AdminDashboardPage());
        }

        // Сортировка в DataGrid
        private void dgInstallations_Sorting(object sender, DataGridSortingEventArgs e)
        {
            var column = e.Column;

            // Определение направления сортировки
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

            // Обновление визуальных индикаторов
            foreach (var col in dgInstallations.Columns)
            {
                col.SortDirection = null;
            }
            column.SortDirection = _currentSortDirection;

            e.Handled = true;
            ApplySorting();
        }
    }
}