using NovaCorpSmartHome.ApplicationData;
using NovaCorpSmartHome.Dialogs;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace NovaCorpSmartHome.Pages.Installer.InstallerActiveOrders
{
    /// <summary>
    /// Логика взаимодействия для InstallerActiveOrdersPage.xaml
    /// </summary>
    public partial class InstallerActiveOrdersPage : Page, INotifyPropertyChanged
    {
        private List<Installations> _allOrders;
        private List<Installations> _filteredOrders;
        private string _currentSortColumn = "PlanDate";
        private ListSortDirection _currentSortDirection = ListSortDirection.Ascending;

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public InstallerActiveOrdersPage()
        {
            InitializeComponent();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadOrdersAsync();
        }

        // Асинхронная загрузка заказов
        private async Task LoadOrdersAsync()
        {
            try
            {
                ShowLoading(true);

                await Task.Run(() =>
                {
                    int currentInstallerId = AppConnect.CurrentEmployee.Id;

                    // Загружаем заказы со статусами: Ожидает установки, Назначена, В работе
                    _allOrders = AppConnect.modelOdb.Installations
                        .Include("Orders")
                        .Include("Orders.Customers")
                        .Include("Orders.OrderItems")
                        .Include("Orders.OrderItems.Products")
                        .Where(i => i.InstallerId == currentInstallerId &&
                                   (i.Status == "Ожидает установки" ||
                                    i.Status == "Назначена" ||
                                    i.Status == "В работе"))
                        .OrderBy(i => i.PlanDate)
                        .ToList();
                });

                _filteredOrders = new List<Installations>(_allOrders);
                ApplySorting();
                UpdateStatistics();
                ShowLoading(false);

                if (_filteredOrders == null || _filteredOrders.Count == 0)
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
                MessageBox.Show($"Ошибка загрузки заказов: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Применение сортировки
        private void ApplySorting()
        {
            if (_filteredOrders == null || _filteredOrders.Count == 0)
            {
                dgOrders.ItemsSource = _filteredOrders;
                return;
            }

            IEnumerable<Installations> sorted = _filteredOrders;

            switch (_currentSortColumn)
            {
                case "Orders.Id":
                    sorted = _currentSortDirection == ListSortDirection.Ascending
                        ? _filteredOrders.OrderBy(i => i.Orders.Id)
                        : _filteredOrders.OrderByDescending(i => i.Orders.Id);
                    break;
                case "Orders.Customers.LastName":
                    sorted = _currentSortDirection == ListSortDirection.Ascending
                        ? _filteredOrders.OrderBy(i => i.Orders?.Customers?.LastName)
                        : _filteredOrders.OrderByDescending(i => i.Orders?.Customers?.LastName);
                    break;
                case "PlanDate":
                    sorted = _currentSortDirection == ListSortDirection.Ascending
                        ? _filteredOrders.OrderBy(i => i.PlanDate)
                        : _filteredOrders.OrderByDescending(i => i.PlanDate);
                    break;
                case "StartDate":
                    sorted = _currentSortDirection == ListSortDirection.Ascending
                        ? _filteredOrders.OrderBy(i => i.StartDate)
                        : _filteredOrders.OrderByDescending(i => i.StartDate);
                    break;
                case "Status":
                    sorted = _currentSortDirection == ListSortDirection.Ascending
                        ? _filteredOrders.OrderBy(i => i.Status)
                        : _filteredOrders.OrderByDescending(i => i.Status);
                    break;
                default:
                    sorted = _filteredOrders.OrderBy(i => i.PlanDate);
                    break;
            }

            dgOrders.ItemsSource = sorted.ToList();
        }

        // Обновление статистики
        private void UpdateStatistics()
        {
            if (_filteredOrders == null) return;

            var total = _filteredOrders.Count;
            var pending = _filteredOrders.Count(i => i.Status == "Ожидает установки" || i.Status == "Назначена");
            var inProgress = _filteredOrders.Count(i => i.Status == "В работе");

            txtStatistics.Text = $"📊 Всего: {total} | ⏳ Ожидают: {pending} | 🔧 В работе: {inProgress}";
        }

        // Показать/скрыть индикатор загрузки
        private void ShowLoading(bool show)
        {
            if (loadingPanel != null) loadingPanel.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            if (dgOrders != null) dgOrders.Visibility = show ? Visibility.Collapsed : Visibility.Visible;
        }

        // Показать/скрыть пустое состояние
        private void ShowEmpty(bool show)
        {
            if (emptyPanel != null) emptyPanel.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            if (dgOrders != null && !show) dgOrders.Visibility = Visibility.Visible;
        }

        // --- НОВЫЙ МЕТОД: Открытие деталей заказа ---
        private void btnDetails_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var installation = button?.Tag as Installations;

            if (installation == null || installation.Orders == null) return;

            var order = installation.Orders;
            var customer = order.Customers;

            // Формируем список товаров для отображения
            var itemsList = new System.Collections.ObjectModel.ObservableCollection<string>();
            if (order.OrderItems != null)
            {
                foreach (var item in order.OrderItems)
                {
                    if (item.Products != null)
                    {
                        itemsList.Add($"{item.Products.Name} (Кол-во: {item.Quantity})");
                    }
                }
            }

            // Показываем диалог с деталями
            var dialog = new OrderDetailsDialog
            {
                OrderId = order.Id,
                ClientName = $"{customer.LastName} {customer.FirstName}",
                ClientPhone = customer.Phone,
                ClientAddress = customer.Address,
                OrderItems = itemsList,
                ManagerComment = order.Notes ?? "Комментариев от менеджера нет."
            };

            dialog.ShowDialog();
        }

        // --- ОБНОВЛЕННЫЙ МЕТОД: Действие (Начать/Завершить) ---
        private async void btnAction_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var installation = button?.Tag as Installations;

            if (installation == null) return;

            try
            {
                // Логика НАЧАЛА работ
                if (installation.Status == "Ожидает установки" || installation.Status == "Назначена")
                {
                    var result = MessageBox.Show("Вы действительно хотите начать выполнение заказа?",
                        "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        await Task.Run(() =>
                        {
                            installation.Status = "В работе";
                            installation.StartDate = DateTime.Now;

                            if (installation.Orders != null)
                            {
                                installation.Orders.Status = "В работе";
                            }

                            AppConnect.modelOdb.SaveChanges();
                        });

                        MessageBox.Show("Статус изменен на «В работе»", "Успех",
                            MessageBoxButton.OK, MessageBoxImage.Information);

                        await LoadOrdersAsync();
                    }
                }
                // Логика ЗАВЕРШЕНИЯ работ
                else if (installation.Status == "В работе")
                {
                    var completeDialog = new CompleteWorkDialog();

                    if (completeDialog.ShowDialog() == true)
                    {
                        string installerComment = completeDialog.Comment;
                        string filePath = completeDialog.FilePath; // Полный путь к файлу на диске пользователя

                        string savedFilePath = "";

                        // Если файл был выбран
                        if (!string.IsNullOrEmpty(filePath))
                        {
                            try
                            {
                                string fileName = System.IO.Path.GetFileName(filePath);
                                string destDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Attachments");

                                if (!System.IO.Directory.Exists(destDir))
                                {
                                    System.IO.Directory.CreateDirectory(destDir);
                                }

                                string destPath = System.IO.Path.Combine(destDir, fileName);
                                System.IO.File.Copy(filePath, destPath, true); // Копируем файл в папку приложения

                                // Сохраняем относительный путь для БД
                                savedFilePath = $"Attachments\\{fileName}";
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show($"Ошибка копирования файла: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                                // Если ошибка копирования, продолжаем без файла, но статус меняем
                            }
                        }

                        await Task.Run(() =>
                        {
                            installation.Status = "Завершена";
                            installation.CompletionDate = DateTime.Now;
                            installation.InstallerNotes = installerComment;

                            // ✅ ВАЖНО: Записываем путь в базу данных
                            // Убедитесь, что свойство называется именно AttachmentPath в вашей модели EF
                            if (!string.IsNullOrEmpty(savedFilePath))
                            {
                                installation.AttachmentPath = savedFilePath;
                            }

                            if (installation.Orders != null)
                            {
                                installation.Orders.Status = "Закрыт";
                            }

                            AppConnect.modelOdb.SaveChanges();
                        });

                        MessageBox.Show("Установка завершена.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        await LoadOrdersAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обновлении статуса: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Кнопка назад
        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService?.CanGoBack == true)
                NavigationService.GoBack();
            else
                NavigationService?.Navigate(new Pages.Installer.InstallerDashboardPage());
        }

        // Сортировка по клику на заголовок
        private void dgOrders_Sorting(object sender, DataGridSortingEventArgs e)
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

            foreach (var col in dgOrders.Columns)
            {
                col.SortDirection = null;
            }
            column.SortDirection = _currentSortDirection;

            e.Handled = true;
            ApplySorting();
        }
    }
}