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

namespace NovaCorpSmartHome.Pages.Manager.ManagerOrderHistory
{
    /// <summary>
    /// Логика взаимодействия для ManagerOrderHistoryPage.xaml
    /// </summary>
    public partial class ManagerOrderHistoryPage : Page
    {
        private Customers _currentClient;
        private Orders _selectedOrder;

        public ManagerOrderHistoryPage()
        {
            InitializeComponent();
            ClearSearch();
        }

        private void ClearSearch()
        {
            txtPhone.Text = "";
            dgOrders.ItemsSource = null;
            borderOrderDetails.Visibility = Visibility.Collapsed;
            gridNoSelection.Visibility = Visibility.Visible;
            borderStatistics.Visibility = Visibility.Collapsed;
            txtOrdersCount.Text = "0";
        }

        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            string phone = txtPhone.Text?.Trim();

            if (string.IsNullOrWhiteSpace(phone))
            {
                MessageBox.Show("Введите номер телефона клиента!",
                              "Ошибка",
                              MessageBoxButton.OK,
                              MessageBoxImage.Warning);
                return;
            }

            try
            {
                string cleanPhone = new string(phone.Where(c => char.IsDigit(c)).ToArray());

                var client = AppConnect.modelOdb.Customers
                    .ToList()
                    .FirstOrDefault(c => new string(c.Phone?.Where(ch => char.IsDigit(ch)).ToArray()) == cleanPhone);

                if (client == null)
                {
                    MessageBox.Show("Клиент с таким номером не найден.",
                                  "Информация",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Information);
                    ClearSearch();
                    return;
                }

                _currentClient = client;

                var orders = AppConnect.modelOdb.Orders
                    .Where(o => o.CustomerId == client.Id)
                    .OrderByDescending(o => o.OrderDate)
                    .ToList();

                dgOrders.ItemsSource = orders;
                txtOrdersCount.Text = orders.Count.ToString();
                borderOrderDetails.Visibility = Visibility.Collapsed;
                gridNoSelection.Visibility = orders.Any() ? Visibility.Collapsed : Visibility.Visible;

                if (orders.Any())
                {
                    borderStatistics.Visibility = Visibility.Visible;
                    UpdateStatistics(orders);
                }
                else
                {
                    borderStatistics.Visibility = Visibility.Collapsed;
                    MessageBox.Show("У клиента нет заказов.",
                                  "Информация",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при поиске заказов: {ex.Message}",
                              "Ошибка",
                              MessageBoxButton.OK,
                              MessageBoxImage.Error);
            }
        }

        private void UpdateStatistics(List<Orders> orders)
        {
            if (orders.Any())
            {
                decimal totalSum = orders.Sum(o => o.TotalAmount);
                txtTotalOrders.Text = $"Всего заказов: {orders.Count}";
                txtTotalSum.Text = $"Общая сумма: {totalSum:N0} ₽";
            }
        }

        private void dgOrders_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgOrders.SelectedItem is Orders selectedOrder)
            {
                try
                {
                    _selectedOrder = selectedOrder;

                    var orderItems = AppConnect.modelOdb.OrderItems
                        .Where(oi => oi.OrderId == selectedOrder.Id)
                        .ToList()
                        .Select(oi => new OrderItemViewModel
                        {
                            Product = AppConnect.modelOdb.Products.First(p => p.Id == oi.ProductId),
                            Quantity = oi.Quantity,
                            UnitPrice = oi.UnitPrice
                        })
                        .ToList();

                    dgOrderItems.ItemsSource = orderItems;

                    txtOrderId.Text = selectedOrder.Id.ToString();
                    txtOrderStatus.Text = selectedOrder.Status;
                    txtOrderDate.Text = selectedOrder.OrderDate.ToString("dd.MM.yyyy HH:mm");
                    txtPaymentDate.Text = selectedOrder.PaymentDate?.ToString("dd.MM.yyyy HH:mm") ?? "Не оплачен";
                    txtOrderTotal.Text = $"{selectedOrder.TotalAmount:N0} ₽";

                    btnConfirmPayment.IsEnabled = (selectedOrder.Status == "Оформлен");

                    borderOrderDetails.Visibility = Visibility.Visible;
                    gridNoSelection.Visibility = Visibility.Collapsed;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при загрузке состава заказа: {ex.Message}",
                                  "Ошибка",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Error);
                }
            }
        }

        private void btnConfirmPayment_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedOrder == null) return;

            try
            {
                if (_selectedOrder.Status != "Оформлен")
                {
                    MessageBox.Show("Статус заказа можно изменить только из 'Оформлен' в 'Оплачен'!",
                                  "Ошибка",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Warning);
                    return;
                }

                var result = MessageBox.Show(
                    "Подтвердить оплату заказа?\n\n" +
                    $"Заказ №{_selectedOrder.Id}\n" +
                    $"Сумма: {_selectedOrder.TotalAmount:N0} ₽\n\n" +
                    "Товары будут списаны со склада. Отменить это действие будет невозможно!",
                    "Подтверждение оплаты",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    _selectedOrder.Status = "Оплачен";
                    _selectedOrder.PaymentDate = DateTime.Now;

                    AppConnect.modelOdb.SaveChanges();

                    // Вызываем событие обновления статистики
                    AppEvents.OnStatisticsChanged();

                    txtOrderStatus.Text = _selectedOrder.Status;
                    txtPaymentDate.Text = _selectedOrder.PaymentDate?.ToString("dd.MM.yyyy HH:mm");
                    btnConfirmPayment.IsEnabled = false;

                    dgOrders.Items.Refresh();

                    MessageBox.Show($"Заказ №{_selectedOrder.Id} успешно оплачен!\n" +
                                  "Товары списаны со склада.",
                                  "Успех",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при подтверждении оплаты: {ex.Message}",
                              "Ошибка",
                              MessageBoxButton.OK,
                              MessageBoxImage.Error);
            }
        }

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
    }

    public class OrderItemViewModel
    {
        public Products Product { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Total => Quantity * UnitPrice;
    }
}