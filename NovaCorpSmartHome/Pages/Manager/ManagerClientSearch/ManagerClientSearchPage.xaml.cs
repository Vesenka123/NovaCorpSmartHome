using NovaCorpSmartHome.ApplicationData;
using NovaCorpSmartHome.Pages.Manager.ManagerOrderCreate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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

namespace NovaCorpSmartHome.Pages.Manager.ManagerClientSearch
{
    /// <summary>
    /// Логика взаимодействия для ManagerClientSearchPage.xaml
    /// </summary>
    public partial class ManagerClientSearchPage : Page
    {
        private Customers _foundCustomer;
        private Customers _currentClient;

        public ManagerClientSearchPage()
        {
            InitializeComponent();

            // Подписываемся на события загрузки и изменения текста
            Loaded += ManagerClientSearchPage_Loaded;

            // Инициализируем плейсхолдеры
            UpdatePlaceholders();
        }

        private void ManagerClientSearchPage_Loaded(object sender, RoutedEventArgs e)
        {
            // Фокус на поле телефона при загрузке
            txtPhone.Focus();
        }

        // Обработчик изменения текста во всех полях
        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdatePlaceholders();
        }

        // Обновление видимости плейсхолдеров
        private void UpdatePlaceholders()
        {
            try
            {
                // Фамилия
                if (PlaceholderLastName != null)
                    PlaceholderLastName.Visibility = string.IsNullOrEmpty(txtLastName?.Text)
                        ? Visibility.Visible : Visibility.Collapsed;

                // Имя
                if (PlaceholderFirstName != null)
                    PlaceholderFirstName.Visibility = string.IsNullOrEmpty(txtFirstName?.Text)
                        ? Visibility.Visible : Visibility.Collapsed;

                // Отчество
                if (PlaceholderMiddleName != null)
                    PlaceholderMiddleName.Visibility = string.IsNullOrEmpty(txtMiddleName?.Text)
                        ? Visibility.Visible : Visibility.Collapsed;

                // Адрес
                if (PlaceholderAddress != null)
                    PlaceholderAddress.Visibility = string.IsNullOrEmpty(txtAddress?.Text)
                        ? Visibility.Visible : Visibility.Collapsed;

                // Email
                if (PlaceholderEmail != null)
                    PlaceholderEmail.Visibility = string.IsNullOrEmpty(txtEmail?.Text)
                        ? Visibility.Visible : Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                // Игнорируем ошибки при инициализации
                System.Diagnostics.Debug.WriteLine($"Ошибка обновления плейсхолдеров: {ex.Message}");
            }
        }

        /// <summary>
        /// Проверяет, является ли номер телефона корректным (Россия)
        /// </summary>
        private bool IsValidPhoneNumber(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return false;

            // Очищаем номер от всех нецифровых символов
            string digits = new string(phone.Where(c => char.IsDigit(c)).ToArray());

            // Проверка длины (РФ: 10 цифр без кода страны или 11 с кодом)
            if (digits.Length == 10)
            {
                // 10 цифр - предполагаем, что это код без 7/8 в начале
                return true;
            }
            else if (digits.Length == 11)
            {
                // 11 цифр - проверяем, что начинается с 7 или 8
                return digits.StartsWith("7") || digits.StartsWith("8");
            }

            return false;
        }

        /// <summary>
        /// Возвращает очищенный номер телефона (только цифры)
        /// </summary>
        private string GetCleanPhoneNumber(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return string.Empty;

            return new string(phone.Where(c => char.IsDigit(c)).ToArray());
        }

        /// <summary>
        /// Форматирует номер телефона в стандартный вид
        /// </summary>
        private string FormatPhoneNumber(string phone)
        {
            try
            {
                if (string.IsNullOrEmpty(phone)) return phone;

                // Убираем все нецифровые символы
                string digits = new string(phone.Where(c => char.IsDigit(c)).ToArray());

                if (digits.Length == 11)
                {
                    if (digits.StartsWith("7") || digits.StartsWith("8"))
                    {
                        // Формат: +7 (XXX) XXX-XX-XX
                        return $"+7 ({digits.Substring(1, 3)}) {digits.Substring(4, 3)}-{digits.Substring(7, 2)}-{digits.Substring(9, 2)}";
                    }
                }
                else if (digits.Length == 10)
                {
                    // Формат: (XXX) XXX-XX-XX
                    return $"({digits.Substring(0, 3)}) {digits.Substring(3, 3)}-{digits.Substring(6, 2)}-{digits.Substring(8, 2)}";
                }

                return phone;
            }
            catch
            {
                return phone;
            }
        }

        // Поиск клиента по телефону
        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string phone = txtPhone.Text.Trim();

                if (string.IsNullOrEmpty(phone))
                {
                    MessageBox.Show("Введите номер телефона для поиска",
                                   "Предупреждение",
                                   MessageBoxButton.OK,
                                   MessageBoxImage.Warning);
                    return;
                }

                // Проверка валидности номера
                if (!IsValidPhoneNumber(phone))
                {
                    MessageBox.Show("Введите корректный номер телефона (10 или 11 цифр)",
                                   "Неверный формат",
                                   MessageBoxButton.OK,
                                   MessageBoxImage.Warning);
                    return;
                }

                // Очищаем номер от лишних символов для поиска
                string cleanPhone = GetCleanPhoneNumber(phone);

                // Ищем клиента в базе данных
                var customer = AppConnect.modelOdb.Customers
                    .ToList() // Загружаем в память для сложной замены
                    .FirstOrDefault(c => GetCleanPhoneNumber(c.Phone) == cleanPhone);

                if (customer != null)
                {
                    _foundCustomer = customer;
                    _currentClient = customer;

                    // Заполняем поля данными найденного клиента
                    txtLastName.Text = customer.LastName;
                    txtFirstName.Text = customer.FirstName;
                    txtMiddleName.Text = customer.MiddleName;
                    txtAddress.Text = customer.Address;
                    txtEmail.Text = customer.Email;

                    // Форматируем номер для отображения
                    txtPhone.Text = FormatPhoneNumber(phone);

                    MessageBox.Show($"Клиент найден: {customer.LastName} {customer.FirstName}",
                                   "Успешно",
                                   MessageBoxButton.OK,
                                   MessageBoxImage.Information);

                    // Можно сразу перейти к оформлению заказа
                    var result = MessageBox.Show("Перейти к оформлению заказа для этого клиента?",
                                                "Подтверждение",
                                                MessageBoxButton.YesNo,
                                                MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        AppFrame.FrameMain.Navigate(new ManagerOrderCreatePage(customer));
                    }
                }
                else
                {
                    MessageBox.Show("Клиент с таким номером телефона не найден. Заполните данные для создания нового клиента.",
                                   "Клиент не найден",
                                   MessageBoxButton.OK,
                                   MessageBoxImage.Information);

                    // Очищаем поля для нового клиента
                    ClearCustomerFields();

                    // Форматируем и оставляем телефон
                    txtPhone.Text = FormatPhoneNumber(phone);
                    _currentClient = null;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при поиске клиента: {ex.Message}",
                               "Ошибка",
                               MessageBoxButton.OK,
                               MessageBoxImage.Error);
            }
        }

        // Создание нового клиента
        private void btnCreateClient_Click(object sender, RoutedEventArgs e)
        {
            string phone = txtPhone.Text?.Trim();

            if (string.IsNullOrWhiteSpace(phone))
            {
                MessageBox.Show("Номер телефона обязателен!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Проверка валидности номера
            if (!IsValidPhoneNumber(phone))
            {
                MessageBox.Show("Введите корректный номер телефона (10 или 11 цифр)",
                               "Неверный формат",
                               MessageBoxButton.OK,
                               MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtLastName.Text) ||
                string.IsNullOrWhiteSpace(txtFirstName.Text) ||
                string.IsNullOrWhiteSpace(txtAddress.Text))
            {
                MessageBox.Show("Заполните ФИО и адрес клиента!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Очищаем номер для поиска в БД
                string cleanPhone = GetCleanPhoneNumber(phone);

                // Проверяем, не появился ли клиент с таким телефоном параллельно
                var existing = AppConnect.modelOdb.Customers
                    .ToList()
                    .FirstOrDefault(c => GetCleanPhoneNumber(c.Phone) == cleanPhone);

                if (existing != null && (_currentClient == null || existing.Id != _currentClient.Id))
                {
                    MessageBox.Show("Клиент с таким номером уже существует!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                Customers client;
                if (_currentClient == null)
                {
                    // Создаём НОВОГО клиента
                    client = new Customers
                    {
                        Phone = FormatPhoneNumber(phone), // Сохраняем в отформатированном виде
                        LastName = txtLastName.Text.Trim(),
                        FirstName = txtFirstName.Text.Trim(),
                        MiddleName = string.IsNullOrWhiteSpace(txtMiddleName.Text) ? null : txtMiddleName.Text.Trim(),
                        Address = txtAddress.Text.Trim(),
                        Email = string.IsNullOrWhiteSpace(txtEmail.Text) ? null : txtEmail.Text.Trim()
                    };

                    AppConnect.modelOdb.Customers.Add(client);
                }
                else
                {
                    // Обновляем существующего
                    client = _currentClient;
                    client.Phone = FormatPhoneNumber(phone);
                    client.LastName = txtLastName.Text.Trim();
                    client.FirstName = txtFirstName.Text.Trim();
                    client.MiddleName = string.IsNullOrWhiteSpace(txtMiddleName.Text) ? null : txtMiddleName.Text.Trim();
                    client.Address = txtAddress.Text.Trim();
                    client.Email = string.IsNullOrWhiteSpace(txtEmail.Text) ? null : txtEmail.Text.Trim();
                }

                AppConnect.modelOdb.SaveChanges();
                MessageBox.Show("Данные клиента сохранены!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                // Переход к заказу
                AppFrame.FrameMain.Navigate(new ManagerOrderCreatePage(client));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении клиента:\n{ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Очистка полей клиента
        private void ClearCustomerFields()
        {
            txtLastName.Text = "";
            txtFirstName.Text = "";
            txtMiddleName.Text = "";
            txtAddress.Text = "";
            txtEmail.Text = "";
            _foundCustomer = null;
        }

        // Очистка всех полей
        private void ClearAllFields()
        {
            txtPhone.Text = "";
            ClearCustomerFields();
            txtPhone.Focus();
        }

        // Назад в меню
        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            if (AppFrame.FrameMain.CanGoBack)
            {
                AppFrame.FrameMain.GoBack();
            }
            else
            {
                AppFrame.FrameMain.Navigate(new Pages.Manager.ManagerDashboard.ManagerDashboardPage());
            }
        }

        // Обработка нажатия Enter в поле телефона
        private void txtPhone_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                btnSearch_Click(sender, e);
            }
        }

        // Форматирование телефона при потере фокуса
        private void txtPhone_LostFocus(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(txtPhone.Text))
            {
                string formatted = FormatPhoneNumber(txtPhone.Text);
                txtPhone.Text = formatted;
            }
        }
    }
}