using NovaCorpSmartHome.ApplicationData;
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

namespace NovaCorpSmartHome.Pages.Authorization
{
    /// <summary>
    /// Логика взаимодействия для AuthorizationPage.xaml
    /// </summary>
    public partial class AuthorizationPage : Page
    {
        public AuthorizationPage()
        {
            InitializeComponent();
        }

        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtLogin.Text) ||
                    string.IsNullOrWhiteSpace(txtPassword.Password))
                {
                    MessageBox.Show("Введите логин и пароль!",
                                   "Ошибка авторизации",
                                   MessageBoxButton.OK,
                                   MessageBoxImage.Warning);
                    return;
                }

                var user = AppConnect.modelOdb.Employees
                    .FirstOrDefault(u => u.Login == txtLogin.Text &&
                                        u.Password == txtPassword.Password);

                if (user != null)
                {
                    AppConnect.CurrentEmployee = user;

                    MessageBox.Show($"Добро пожаловать, {user.LastName} {user.FirstName}!",
                                   "Успешный вход",
                                   MessageBoxButton.OK,
                                   MessageBoxImage.Information);

                    // НАВИГАЦИЯ ПО РОЛЯМ (ИСПРАВЛЕНО!)
                    switch (user.Role)
                    {
                        case "Администратор":
                            AppFrame.FrameMain.Navigate(new Pages.Admin.AdminDashboard.AdminDashboardPage());
                            break;
                        case "Менеджер":
                            AppFrame.FrameMain.Navigate(new Pages.Manager.ManagerDashboard.ManagerDashboardPage());
                            break;
                        case "Установщик":
                            AppFrame.FrameMain.Navigate(new Pages.Installer.InstallerDashboardPage());
                            break;
                        default:
                            // На случай, если роль не распознана — гостевой режим
                            AppFrame.FrameMain.Navigate(new Pages.Guest.GuestCatalog.GuestCatalogPage());
                            break;
                    }
                }
                else
                {
                    MessageBox.Show("Неверный логин или пароль!",
                                   "Ошибка авторизации",
                                   MessageBoxButton.OK,
                                   MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при авторизации: {ex.Message}",
                               "Ошибка",
                               MessageBoxButton.OK,
                               MessageBoxImage.Error);
            }
        }
        private void btnGuest_Click(object sender, RoutedEventArgs e)
        {
            // Вход как гость
            AppConnect.CurrentEmployee = null;
            AppFrame.FrameMain.Navigate(new Pages.Guest.GuestCatalog.GuestCatalogPage());
        }

        // Обработчик для клавиши Enter
        private void txtPassword_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                btnLogin_Click(sender, e);
            }
        }

        private void txtLogin_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                txtPassword.Focus();
            }
        }
    }
}
