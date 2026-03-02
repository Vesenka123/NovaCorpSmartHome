using NovaCorpSmartHome.ApplicationData;
using NovaCorpSmartHome.Pages.Authorization;
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

namespace NovaCorpSmartHome.Pages.Admin.AdminDashboard
{
    /// <summary>
    /// Логика взаимодействия для AdminDashboardPage.xaml
    /// </summary>
    public partial class AdminDashboardPage : Page
    {
        public AdminDashboardPage()
        {
            InitializeComponent();
            LoadStatistics();
        }

        private void LoadStatistics()
        {
            try
            {
                // Статистика товаров
                int productsCount = AppConnect.modelOdb.Products.Count();
                txtProductsCount.Text = productsCount.ToString();

                // Статистика категорий
                int categoriesCount = AppConnect.modelOdb.Categories.Count();
                txtCategoriesCount.Text = categoriesCount.ToString();

                // Статистика склада (количество позиций с остатками)
                int stockItemsCount = AppConnect.modelOdb.Stock.Count(s => s.Quantity > 0);
                txtStockItemsCount.Text = stockItemsCount.ToString();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки статистики: {ex.Message}");
            }
        }

        private void CardProducts_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            AppFrame.FrameMain.Navigate(new Pages.Admin.AdminProducts.AdminProductsPage());
        }

        private void CardStock_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            AppFrame.FrameMain.Navigate(new Pages.Admin.AdminStock.AdminStockPage());
        }

        private void CardReports_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Раскомментируйте, когда создадите страницу отчетов
            // AppFrame.FrameMain.Navigate(new Pages.Admin.AdminReports.AdminReportsPage());
            MessageBox.Show("Страница отчетов находится в разработке",
                          "Информация",
                          MessageBoxButton.OK,
                          MessageBoxImage.Information);
        }

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