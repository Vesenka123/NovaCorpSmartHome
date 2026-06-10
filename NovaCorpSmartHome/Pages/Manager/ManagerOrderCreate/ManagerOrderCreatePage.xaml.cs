using NovaCorpSmartHome.ApplicationData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace NovaCorpSmartHome.Pages.Manager.ManagerOrderCreate
{
    public partial class ManagerOrderCreatePage : Page
    {
        private Customers _client;
        private List<OrderItemViewModel> _orderItems = new List<OrderItemViewModel>();

        // Временное хранение заказа между оформлением и оплатой
        private Orders _currentOrder;
        private int? _pendingInstallerId;

        public ManagerOrderCreatePage(Customers client)
        {
            InitializeComponent();
            _client = client;
            txtClientName.Text = $"{client.LastName} {client.FirstName} {client.MiddleName}".Trim();
            txtClientPhone.Text = client.Phone;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LoadCategories();
            LoadInstallers();
            UpdateTotals();
            dpInstallationDate.SelectedDate = DateTime.Now.AddDays(3);
            lstProducts.SelectionChanged += (s, args) => btnAddToOrder.IsEnabled = lstProducts.SelectedItem != null;
        }

        private void LoadCategories()
        {
            var categories = AppConnect.modelOdb.Categories.Where(c => c.ParentId == null).OrderBy(c => c.Name).ToList();
            foreach (var cat in categories)
                treeCategories.Items.Add(CreateCategoryNode(cat));
        }

        private TreeViewItem CreateCategoryNode(Categories category)
        {
            var node = new TreeViewItem { Header = category.Name, Tag = category, IsExpanded = true };
            var children = AppConnect.modelOdb.Categories.Where(c => c.ParentId == category.Id).OrderBy(c => c.Name).ToList();
            foreach (var child in children)
                node.Items.Add(CreateCategoryNode(child));
            return node;
        }

        private void LoadInstallers()
        {
            var installers = AppConnect.modelOdb.Employees.Where(e => e.Role == "Установщик").ToList();
            cmbInstaller.Items.Clear();
            cmbInstaller.Items.Add(new ComboBoxItem { Content = "-- Не назначен --", Tag = null });
            foreach (var emp in installers)
            {
                string name = $"{emp.LastName} {emp.FirstName}";
                if (!string.IsNullOrWhiteSpace(emp.MiddleName)) name += $" {emp.MiddleName}";
                cmbInstaller.Items.Add(new ComboBoxItem { Content = name, Tag = emp.Id });
            }
            cmbInstaller.SelectedIndex = 0;
        }

        private void treeCategories_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is TreeViewItem item && item.Tag is Categories cat)
            {
                txtSelectedCategory.Text = cat.Name;
                var products = AppConnect.modelOdb.Products.Where(p => p.CategoryId == cat.Id && p.IsActive).OrderBy(p => p.Name).ToList();
                lstProducts.ItemsSource = products;
            }
        }

        private void AddSelectedProductToOrder()
        {
            if (lstProducts.SelectedItem is Products product)
            {
                if (product.Type == "Товар")
                {
                    var stock = AppConnect.modelOdb.Stock.FirstOrDefault(s => s.ProductId == product.Id);
                    if (stock == null || stock.Quantity <= 0)
                    {
                        MessageBox.Show($"Товар «{product.Name}» отсутствует на складе!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                var existing = _orderItems.FirstOrDefault(i => i.Product.Id == product.Id);
                if (existing != null) existing.Quantity++;
                else _orderItems.Add(new OrderItemViewModel { Product = product, Quantity = 1, UnitPrice = product.Price });

                RefreshCart();
            }
        }

        private void btnAddToOrder_Click(object sender, RoutedEventArgs e) => AddSelectedProductToOrder();
        private void lstProducts_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e) => AddSelectedProductToOrder();

        private void btnIncreaseQuantity_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is OrderItemViewModel item)
            {
                if (item.Product.Type == "Товар")
                {
                    var stock = AppConnect.modelOdb.Stock.FirstOrDefault(s => s.ProductId == item.Product.Id);
                    if (stock != null && item.Quantity + 1 > stock.Quantity)
                    {
                        MessageBox.Show($"Доступно только {stock.Quantity} шт.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }
                item.Quantity++;
                RefreshCart();
            }
        }

        private void btnDecreaseQuantity_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is OrderItemViewModel item && item.Quantity > 1)
            {
                item.Quantity--;
                RefreshCart();
            }
        }

        private void btnRemoveItem_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is OrderItemViewModel item)
            {
                _orderItems.Remove(item);
                RefreshCart();
            }
        }

        private void btnClearOrder_Click(object sender, RoutedEventArgs e)
        {
            if (_orderItems.Any() && MessageBox.Show("Очистить корзину?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                _orderItems.Clear();
                RefreshCart();
            }
        }

        private void RefreshCart()
        {
            icOrderItems.ItemsSource = null;
            icOrderItems.ItemsSource = _orderItems;
            UpdateTotals();
        }

        private void UpdateTotals()
        {
            txtTotal.Text = $"{_orderItems.Sum(i => i.Total):N0} ₽";
            txtItemsCount.Text = _orderItems.Sum(i => i.Quantity).ToString();
        }

        // ==========================================================
        // ШАГ 1: ОФОРМЛЕНИЕ ЗАКАЗА (статус "Оформлен", БЕЗ списания)
        // ==========================================================
        private void btnSaveOrder_Click(object sender, RoutedEventArgs e)
        {
            if (!_orderItems.Any())
            {
                MessageBox.Show("Добавьте товары в заказ!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                _pendingInstallerId = (cmbInstaller.SelectedItem as ComboBoxItem)?.Tag as int?;

                // 1. Создаём заказ со статусом "Оформлен"
                _currentOrder = new Orders
                {
                    CustomerId = _client.Id,
                    ManagerId = AppConnect.CurrentEmployee.Id,
                    OrderDate = DateTime.Now,
                    Status = "Оформлен",
                    TotalAmount = _orderItems.Sum(i => i.Total),
                    InstallationDate = dpInstallationDate.SelectedDate,
                    Notes = string.IsNullOrWhiteSpace(txtNotes.Text) ? null : txtNotes.Text,
                    PaymentDate = null // ещё не оплачен
                };
                AppConnect.modelOdb.Orders.Add(_currentOrder);
                AppConnect.modelOdb.SaveChanges();

                // 2. Сохраняем позиции заказа
                foreach (var item in _orderItems)
                {
                    AppConnect.modelOdb.OrderItems.Add(new OrderItems
                    {
                        OrderId = _currentOrder.Id,
                        ProductId = item.Product.Id,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice
                    });
                }
                AppConnect.modelOdb.SaveChanges();

                // 3. ⚠️ ТОВАРЫ СО СКЛАДА НЕ СПИСЫВАЕМ — ждём оплаты!

                // 4. Показываем окно-заглушку оплаты
                ShowPaymentOverlay();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка оформления: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ==========================================================
        // ПОКАЗ ОВЕРЛЕЯ ОПЛАТЫ
        // ==========================================================
        private void ShowPaymentOverlay()
        {
            txtPaymentOrderNumber.Text = $"Заказ №{_currentOrder.Id} от {_currentOrder.OrderDate:dd.MM.yyyy HH:mm}";
            txtPaymentAmount.Text = $"{_currentOrder.TotalAmount:N0} ₽";
            txtPaymentClient.Text = $"{_client.LastName} {_client.FirstName} {_client.MiddleName}".Trim();
            txtPaymentPhone.Text = _client.Phone;

            overlayPayment.Visibility = Visibility.Visible;
            btnConfirmPayment.Focus();
        }

        private void HidePaymentOverlay()
        {
            overlayPayment.Visibility = Visibility.Collapsed;
        }

        // ==========================================================
        // ШАГ 2: ПОДТВЕРЖДЕНИЕ ОПЛАТЫ (статус "Оплачен" + списание)
        // ==========================================================
        private void btnConfirmPayment_Click(object sender, RoutedEventArgs e)
        {
            if (_currentOrder == null) return;

            try
            {
                // 1. Меняем статус на "Оплачен"
                _currentOrder.Status = "Оплачен";
                _currentOrder.PaymentDate = DateTime.Now;

                // 2. СПИСЫВАЕМ товары со склада
                foreach (var item in _orderItems)
                {
                    if (item.Product.Type == "Товар")
                    {
                        var stock = AppConnect.modelOdb.Stock.FirstOrDefault(s => s.ProductId == item.Product.Id);
                        if (stock != null)
                        {
                            if (stock.Quantity < item.Quantity)
                            {
                                MessageBox.Show($"Недостаточно товара «{item.Product.Name}» на складе!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                                return;
                            }
                            stock.Quantity -= item.Quantity;
                        }
                    }
                }

                AppConnect.modelOdb.SaveChanges();

                // 3. Создаём запись об установке (если назначен установщик)
                if (_pendingInstallerId.HasValue)
                {
                    AppConnect.modelOdb.Installations.Add(new Installations
                    {
                        OrderId = _currentOrder.Id,
                        InstallerId = _pendingInstallerId.Value,
                        PlanDate = dpInstallationDate.SelectedDate ?? DateTime.Now.AddDays(3),
                        Status = "Ожидает установки"
                    });
                    AppConnect.modelOdb.SaveChanges();
                }

                HidePaymentOverlay();

                AppEvents.OnStatisticsChanged();

                MessageBox.Show(
                    $"✅ Заказ №{_currentOrder.Id} успешно оплачен!\n\n" +
                    $"Сумма: {_currentOrder.TotalAmount:N0} ₽\n" +
                    $"Товары списаны со склада.",
                    "Оплата подтверждена",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                // Сбрасываем состояние и возвращаемся
                _currentOrder = null;
                _orderItems.Clear();
                GoBack();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка оплаты: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ==========================================================
        // ОТМЕНА ОПЛАТЫ — удаляем черновик заказа
        // ==========================================================
        private void btnCancelPayment_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Отменить оплату?\n\nЗаказ будет удалён, товары останутся на складе.",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    if (_currentOrder != null)
                    {
                        // Удаляем позиции заказа
                        var items = AppConnect.modelOdb.OrderItems.Where(o => o.OrderId == _currentOrder.Id).ToList();
                        AppConnect.modelOdb.OrderItems.RemoveRange(items);

                        // Удаляем сам заказ
                        AppConnect.modelOdb.Orders.Remove(_currentOrder);
                        AppConnect.modelOdb.SaveChanges();

                        _currentOrder = null;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка отмены: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                HidePaymentOverlay();
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (_orderItems.Any() && MessageBox.Show("Заказ не сохранен. Вернуться?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                return;
            GoBack();
        }

        private void GoBack() => NavigationService?.GoBack();
    }

    public class OrderItemViewModel
    {
        public Products Product { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Total => Quantity * UnitPrice;
    }
}