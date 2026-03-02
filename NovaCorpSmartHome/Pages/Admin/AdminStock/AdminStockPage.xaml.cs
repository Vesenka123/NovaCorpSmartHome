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

namespace NovaCorpSmartHome.Pages.Admin.AdminStock
{
    /// <summary>
    /// Логика взаимодействия для AdminStockPage.xaml
    /// </summary>
    public partial class AdminStockPage : Page
    {
        private List<StockItemViewModel> _allStockItems;
        private CollectionView _view;
        private bool _isEditing = false;

        public AdminStockPage()
        {
            InitializeComponent();

            // Подписываемся на событие изменения статистики
            AppEvents.StatisticsChanged += OnStatisticsChanged;

            LoadStockData();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LoadStockData();
        }

        private void OnStatisticsChanged(object sender, EventArgs e)
        {
            // Обновляем данные при изменении статистики (например, при добавлении товара)
            Dispatcher.Invoke(() => LoadStockData());
        }

        private void LoadStockData()
        {
            try
            {
                // Загружаем данные со склада + связанные товары и категории
                _allStockItems = AppConnect.modelOdb.Stock
                    .Select(s => new StockItemViewModel
                    {
                        ProductId = s.ProductId,
                        Quantity = s.Quantity,
                        Product = s.Products,
                        Category = s.Products.Categories
                    })
                    .OrderBy(x => x.Product.Name)
                    .ToList();

                _view = (CollectionView)CollectionViewSource.GetDefaultView(_allStockItems);
                dgStock.ItemsSource = _view;

                UpdateStatistics();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки склада: {ex.Message}",
                              "Ошибка",
                              MessageBoxButton.OK,
                              MessageBoxImage.Error);
            }
        }

        private void UpdateStatistics()
        {
            try
            {
                if (_allStockItems == null) return;

                int totalItems = _allStockItems.Count;
                int lowStock = _allStockItems.Count(x => x.Quantity > 0 && x.Quantity <= 3);
                int outOfStock = _allStockItems.Count(x => x.Quantity == 0);

                txtTotalItems.Text = totalItems.ToString();
                txtLowStock.Text = lowStock.ToString();
                txtOutOfStock.Text = outOfStock.ToString();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка обновления статистики: {ex.Message}");
            }
        }

        private void ApplyFilters()
        {
            if (_view == null || _isEditing) return;

            try
            {
                _view.Filter = null;

                string typeFilter = (cmbTypeFilter.SelectedItem as ComboBoxItem)?.Content.ToString();
                if (!string.IsNullOrEmpty(typeFilter) && typeFilter != "Все типы")
                {
                    _view.Filter = item => ((StockItemViewModel)item).Product.Type == typeFilter;
                }

                string stockFilter = (cmbStockFilter.SelectedItem as ComboBoxItem)?.Content.ToString();
                if (!string.IsNullOrEmpty(stockFilter) && stockFilter != "Все остатки")
                {
                    var currentFilter = _view.Filter;

                    if (stockFilter == "Только в наличии (>0)")
                    {
                        _view.Filter = item => ((StockItemViewModel)item).Quantity > 0;
                    }
                    else if (stockFilter == "Малый остаток (≤3)")
                    {
                        _view.Filter = item => ((StockItemViewModel)item).Quantity <= 3 && ((StockItemViewModel)item).Quantity > 0;
                    }
                    else if (stockFilter == "Отсутствуют (0)")
                    {
                        _view.Filter = item => ((StockItemViewModel)item).Quantity == 0;
                    }
                }

                if (!string.IsNullOrWhiteSpace(txtSearch.Text))
                {
                    string searchText = txtSearch.Text.ToLower();
                    var currentFilter = _view.Filter;

                    _view.Filter = item =>
                    {
                        var stockItem = (StockItemViewModel)item;
                        bool matchesSearch = stockItem.Product.Name.ToLower().Contains(searchText) ||
                                           (stockItem.Category?.Name?.ToLower().Contains(searchText) ?? false);

                        if (currentFilter != null)
                            return currentFilter(item) && matchesSearch;

                        return matchesSearch;
                    };
                }

                dgStock.Items.Refresh();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка применения фильтров: {ex.Message}");
            }
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void cmbTypeFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void cmbStockFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void dgStock_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            _isEditing = true;
        }

        private void dgStock_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit && e.Column.Header.ToString() == "Остаток")
            {
                if (e.Row.Item is StockItemViewModel item && e.EditingElement is TextBox textBox)
                {
                    if (int.TryParse(textBox.Text, out int newQuantity))
                    {
                        if (newQuantity < 0)
                        {
                            MessageBox.Show("Остаток не может быть отрицательным!",
                                          "Ошибка",
                                          MessageBoxButton.OK,
                                          MessageBoxImage.Warning);
                            e.Cancel = true;
                            textBox.Text = item.Quantity.ToString();
                            _isEditing = false;
                            return;
                        }

                        try
                        {
                            var stockRecord = AppConnect.modelOdb.Stock
                                .FirstOrDefault(s => s.ProductId == item.ProductId);

                            if (stockRecord != null)
                            {
                                stockRecord.Quantity = newQuantity;
                                AppConnect.modelOdb.SaveChanges();
                                item.Quantity = newQuantity;

                                UpdateStatistics();
                                AppEvents.OnStatisticsChanged();
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Ошибка сохранения: {ex.Message}",
                                          "Ошибка",
                                          MessageBoxButton.OK,
                                          MessageBoxImage.Error);
                            e.Cancel = true;
                            textBox.Text = item.Quantity.ToString();
                        }
                    }
                    else
                    {
                        MessageBox.Show("Введите целое число!",
                                      "Ошибка",
                                      MessageBoxButton.OK,
                                      MessageBoxImage.Warning);
                        e.Cancel = true;
                        textBox.Text = item.Quantity.ToString();
                    }
                }
            }

            _isEditing = false;
        }

        private void dgStock_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            _isEditing = false;
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            if (_isEditing)
            {
                MessageBox.Show("Завершите редактирование перед обновлением",
                              "Информация",
                              MessageBoxButton.OK,
                              MessageBoxImage.Information);
                return;
            }

            LoadStockData();
            cmbTypeFilter.SelectedIndex = 0;
            cmbStockFilter.SelectedIndex = 0;
            txtSearch.Text = "";
            ApplyFilters();
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            // Отписываемся от события при уходе со страницы
            AppEvents.StatisticsChanged -= OnStatisticsChanged;

            if (AppFrame.FrameMain.CanGoBack)
            {
                AppFrame.FrameMain.GoBack();
            }
            else
            {
                AppFrame.FrameMain.Navigate(new Pages.Admin.AdminDashboard.AdminDashboardPage());
            }
        }
    }

    public class StockItemViewModel
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public Products Product { get; set; }
        public Categories Category { get; set; }
    }
}