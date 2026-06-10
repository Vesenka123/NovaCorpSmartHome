using NovaCorpSmartHome.ApplicationData;
using NovaCorpSmartHome.Pages.Authorization;
using NovaCorpSmartHome.Pages.Installer.InstallerActiveOrders;
using NovaCorpSmartHome.Pages.Installer.InstallerHistory;
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

namespace NovaCorpSmartHome.Pages.Installer
{
    /// <summary>
    /// Логика взаимодействия для InstallerDashboardPage.xaml
    /// </summary>
    public partial class InstallerDashboardPage : Page
    {
        public InstallerDashboardPage()
        {
            InitializeComponent();
            LoadStatistics();
        }

        private void LoadStatistics()
        {
            try
            {
                // Получаем ID текущего установщика
                int currentInstallerId = AppConnect.CurrentEmployee.Id;

                // Считаем заказы по статусам
                // Важно: убедитесь, что строки статусов совпадают с теми, что в БД
                int active = AppConnect.modelOdb.Installations.Count(i => i.InstallerId == currentInstallerId && (i.Status == "В работе" || i.Status == "Ожидает установки"));
                int pending = AppConnect.modelOdb.Installations.Count(i => i.InstallerId == currentInstallerId && i.Status == "Ожидает установки");
                int completed = AppConnect.modelOdb.Installations.Count(i => i.InstallerId == currentInstallerId && i.Status == "Завершена");

                txtActiveCount.Text = active.ToString();
                txtPendingCount.Text = pending.ToString();
                txtCompletedCount.Text = completed.ToString();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки статистики установщика: {ex.Message}");
                txtActiveCount.Text = "0";
                txtPendingCount.Text = "0";
                txtCompletedCount.Text = "0";
            }
        }

        private void btnLogout_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Вы действительно хотите выйти из системы «Новус»?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                AppConnect.CurrentEmployee = null;
                AppFrame.FrameMain.Navigate(new AuthorizationPage());
            }
        }

        private void CardActiveOrders_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Переход на список активных заказов установщика
            // Здесь будет реализована новая логика с кнопкой "Подробнее"
            AppFrame.FrameMain.Navigate(new InstallerActiveOrdersPage());
        }

        private void CardHistory_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Переход в историю выполненных монтажей
            AppFrame.FrameMain.Navigate(new InstallerHistoryPage());
        }
    }
}