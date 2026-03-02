using NovaCorpSmartHome.ApplicationData;
using NovaCorpSmartHome.Pages.Authorization;
using NovaCorpSmartHome.Pages.Guest.GuestCatalog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace NovaCorpSmartHome.Pages.Manager.ManagerDashboard
{
    /// <summary>
    /// Логика взаимодействия для ManagerDashboardPage.xaml
    /// </summary>
    public partial class ManagerDashboardPage : Page
    {
        public ManagerDashboardPage()
        {
            InitializeComponent();

            // Показываем имя текущего менеджера
            if (AppConnect.CurrentEmployee != null)
            {
                txtManagerName.Text = $"{AppConnect.CurrentEmployee.FirstName} {AppConnect.CurrentEmployee.LastName}";
            }

            // Подписываемся на событие изменения статистики
            AppEvents.StatisticsChanged += OnStatisticsChanged;

            // Подписываемся на события загрузки и выгрузки страницы
            this.Loaded += ManagerDashboardPage_Loaded;
            this.Unloaded += ManagerDashboardPage_Unloaded;

            LoadStatistics();
        }

        // Обработчик загрузки страницы
        private void ManagerDashboardPage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadStatistics();
        }

        // Обработчик выгрузки страницы
        private void ManagerDashboardPage_Unloaded(object sender, RoutedEventArgs e)
        {
            // Отписываемся от события при закрытии страницы
            AppEvents.StatisticsChanged -= OnStatisticsChanged;
        }

        // Обработчик события изменения статистики
        private void OnStatisticsChanged(object sender, EventArgs e)
        {
            // Обновляем статистику в UI-потоке
            Dispatcher.Invoke(() => LoadStatistics());
        }

        // Загрузка статистики
        private void LoadStatistics()
        {
            try
            {
                // Статистика клиентов
                int totalClients = AppConnect.modelOdb.Customers.Count();
                txtClientsCount.Text = totalClients.ToString();

                // Статистика заказов
                int оформлен = AppConnect.modelOdb.Orders.Count(o => o.Status == "Оформлен");
                int оплачен = AppConnect.modelOdb.Orders.Count(o => o.Status == "Оплачен");
                int totalOrders = оформлен + оплачен;
                txtOrdersCount.Text = totalOrders.ToString();

                // Статистика товаров на складе
                int totalStockItems = 0;
                if (AppConnect.modelOdb.Stock.Any())
                {
                    totalStockItems = AppConnect.modelOdb.Stock.Sum(s => (int?)s.Quantity) ?? 0;
                }
                txtStockCount.Text = totalStockItems.ToString();

                // Для отладки
                System.Diagnostics.Debug.WriteLine($"=== СТАТИСТИКА ОБНОВЛЕНА {DateTime.Now:HH:mm:ss} ===");
                System.Diagnostics.Debug.WriteLine($"Клиентов: {totalClients}, Заказов: {totalOrders}, Товаров: {totalStockItems}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки статистики: {ex.Message}");

                txtClientsCount.Text = "0";
                txtOrdersCount.Text = "0";
                txtStockCount.Text = "0";
            }
        }

        // Переход к поиску клиента
        private void CardClientSearch_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            AppFrame.FrameMain.Navigate(new Pages.Manager.ManagerClientSearch.ManagerClientSearchPage());
        }

        // Переход к истории заказов
        private void CardOrderHistory_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            AppFrame.FrameMain.Navigate(new Pages.Manager.ManagerOrderHistory.ManagerOrderHistoryPage());
        }

        // Переход к каталогу
        private void CardCatalog_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            AppFrame.FrameMain.Navigate(new Pages.Guest.GuestCatalog.GuestCatalogPage());
            AppConnect.CurrentEmployee = null;
        }

        // Выход из системы
        private void btnLogout_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Вы действительно хотите выйти из системы?",
                                        "Подтверждение",
                                        MessageBoxButton.YesNo,
                                        MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                AppConnect.CurrentEmployee = null;
                AppFrame.FrameMain.Navigate(new AuthorizationPage());
            }
        }
    }
}