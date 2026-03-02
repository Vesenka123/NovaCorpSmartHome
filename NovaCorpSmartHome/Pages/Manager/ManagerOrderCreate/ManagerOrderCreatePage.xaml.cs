using NovaCorpSmartHome.ApplicationData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace NovaCorpSmartHome.Pages.Manager.ManagerOrderCreate
{
    /// <summary>
    /// Логика взаимодействия для ManagerOrderCreatePage.xaml
    /// </summary>
    public partial class ManagerOrderCreatePage : Page
    {
        private Customers _client;
        private List<OrderItemViewModel> _orderItems = new List<OrderItemViewModel>();

        public ManagerOrderCreatePage(Customers client)
        {
            InitializeComponent();
            _client = client ?? throw new ArgumentNullException(nameof(client));

            // Заполняем информацию о клиенте
            txtClientName.Text = $"{client.LastName} {client.FirstName} {client.MiddleName}".Trim();
            txtClientPhone.Text = client.Phone;

            LoadCategories();
            UpdateTotal();

            // Подписываемся на изменение выбора в списке товаров
            lstProducts.SelectionChanged += LstProducts_SelectionChanged;
        }

        private void LoadCategories()
        {
            try
            {
                treeCategories.Items.Clear();

                var categories = AppConnect.modelOdb.Categories
                    .Where(c => c.ParentId == null)
                    .OrderBy(c => c.Name)
                    .ToList();

                foreach (var cat in categories)
                {
                    var item = CreateTreeViewItem(cat);
                    treeCategories.Items.Add(item);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки категорий: {ex.Message}",
                               "Ошибка",
                               MessageBoxButton.OK,
                               MessageBoxImage.Error);
            }
        }

        private TreeViewItem CreateTreeViewItem(Categories category)
        {
            var item = new TreeViewItem
            {
                Header = category.Name,
                Tag = category,
                IsExpanded = true
            };

            var subCats = AppConnect.modelOdb.Categories
                .Where(c => c.ParentId == category.Id)
                .OrderBy(c => c.Name)
                .ToList();

            foreach (var subCat in subCats)
            {
                item.Items.Add(CreateTreeViewItem(subCat));
            }

            return item;
        }

        private void treeCategories_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            try
            {
                if (treeCategories.SelectedItem is TreeViewItem selectedItem)
                {
                    if (selectedItem.Tag is Categories category)
                    {
                        txtSelectedCategory.Text = category.Name;
                        LoadProductsByCategory(category.Id);
                        lstProducts.SelectedItem = null;
                        btnAddToOrder.IsEnabled = false;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки товаров: {ex.Message}",
                               "Ошибка",
                               MessageBoxButton.OK,
                               MessageBoxImage.Error);
            }
        }

        private void LoadProductsByCategory(int categoryId)
        {
            var products = AppConnect.modelOdb.Products
                .Where(p => p.CategoryId == categoryId && p.IsActive)
                .OrderBy(p => p.Name)
                .ToList();

            lstProducts.ItemsSource = products;
        }

        private void LstProducts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            btnAddToOrder.IsEnabled = lstProducts.SelectedItem != null;
        }

        private void btnAddToOrder_Click(object sender, RoutedEventArgs e)
        {
            AddSelectedProductToOrder();
        }

        private void lstProducts_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            AddSelectedProductToOrder();
        }

        private void AddSelectedProductToOrder()
        {
            try
            {
                if (lstProducts.SelectedItem is Products product)
                {
                    // Проверка остатков для товаров
                    if (product.Type == "Товар")
                    {
                        var stock = AppConnect.modelOdb.Stock
                            .FirstOrDefault(s => s.ProductId == product.Id);

                        if (stock == null || stock.Quantity <= 0)
                        {
                            MessageBox.Show($"Товар «{product.Name}» отсутствует на складе!",
                                           "Ошибка",
                                           MessageBoxButton.OK,
                                           MessageBoxImage.Warning);
                            return;
                        }
                    }

                    var existingItem = _orderItems.FirstOrDefault(i => i.Product.Id == product.Id);

                    if (existingItem != null)
                    {
                        existingItem.Quantity++;
                    }
                    else
                    {
                        _orderItems.Add(new OrderItemViewModel
                        {
                            Product = product,
                            Quantity = 1,
                            UnitPrice = product.Price
                        });
                    }

                    RefreshOrderItems();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении товара: {ex.Message}",
                               "Ошибка",
                               MessageBoxButton.OK,
                               MessageBoxImage.Error);
            }
        }

        private void btnRemoveItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                var item = button?.Tag as OrderItemViewModel;

                if (item != null)
                {
                    _orderItems.Remove(item);
                    RefreshOrderItems();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении товара: {ex.Message}",
                               "Ошибка",
                               MessageBoxButton.OK,
                               MessageBoxImage.Error);
            }
        }

        private void btnClearOrder_Click(object sender, RoutedEventArgs e)
        {
            if (_orderItems.Any())
            {
                var result = MessageBox.Show("Очистить состав заказа?",
                                             "Подтверждение",
                                             MessageBoxButton.YesNo,
                                             MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    _orderItems.Clear();
                    RefreshOrderItems();
                }
            }
        }

        private void RefreshOrderItems()
        {
            icOrderItems.ItemsSource = null;
            icOrderItems.ItemsSource = _orderItems;
            UpdateTotal();
        }

        private void UpdateTotal()
        {
            decimal total = _orderItems.Sum(i => i.Total);
            txtTotal.Text = $"{total:N0} ₽";
            int itemsCount = _orderItems.Sum(i => i.Quantity);
            txtItemsCount.Text = itemsCount.ToString();
        }

        private void btnSaveOrder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_orderItems.Any())
                {
                    MessageBox.Show("Добавьте хотя бы один товар или услугу!",
                                   "Ошибка",
                                   MessageBoxButton.OK,
                                   MessageBoxImage.Warning);
                    return;
                }

                var order = new Orders
                {
                    CustomerId = _client.Id,
                    ManagerId = AppConnect.CurrentEmployee.Id,
                    OrderDate = DateTime.Now,
                    Status = "Оформлен",
                    TotalAmount = _orderItems.Sum(i => i.Total),
                    InstallationDate = dpInstallationDate.SelectedDate,
                    Notes = string.IsNullOrWhiteSpace(txtNotes.Text) ? null : txtNotes.Text,
                    PaymentDate = null
                };

                AppConnect.modelOdb.Orders.Add(order);
                AppConnect.modelOdb.SaveChanges();

                foreach (var item in _orderItems)
                {
                    var orderItem = new OrderItems
                    {
                        OrderId = order.Id,
                        ProductId = item.Product.Id,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice
                    };

                    AppConnect.modelOdb.OrderItems.Add(orderItem);

                    if (item.Product.Type == "Товар")
                    {
                        var stock = AppConnect.modelOdb.Stock
                            .FirstOrDefault(s => s.ProductId == item.Product.Id);

                        if (stock != null)
                        {
                            stock.Quantity -= item.Quantity;
                        }
                    }
                }

                AppConnect.modelOdb.SaveChanges();

                // Вызываем событие обновления статистики
                AppEvents.OnStatisticsChanged();

                MessageBox.Show($"Заказ №{order.Id} успешно создан!",
                               "Успех",
                               MessageBoxButton.OK,
                               MessageBoxImage.Information);

                GoBack();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении заказа: {ex.Message}",
                               "Ошибка",
                               MessageBoxButton.OK,
                               MessageBoxImage.Error);
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (_orderItems.Any())
            {
                var result = MessageBox.Show("Заказ не сохранен. Вернуться?",
                                             "Подтверждение",
                                             MessageBoxButton.YesNo,
                                             MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    GoBack();
                }
            }
            else
            {
                GoBack();
            }
        }

        private void GoBack()
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