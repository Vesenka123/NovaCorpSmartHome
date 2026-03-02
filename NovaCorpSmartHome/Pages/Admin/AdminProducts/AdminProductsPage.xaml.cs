using Microsoft.Win32;
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

namespace NovaCorpSmartHome.Pages.Admin.AdminProducts
{
    /// <summary>
    /// Логика взаимодействия для AdminProductsPage.xaml
    /// </summary>
    public partial class AdminProductsPage : Page
    {
        private Products _selectedProduct;
        private string _selectedImagePath;
        private bool _isEditMode = false;

        public AdminProductsPage()
        {
            try
            {
                InitializeComponent();

                // Загружаем данные
                LoadCategories();
                LoadProducts();
                UpdateProductsCount();
                ClearForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при инициализации: {ex.Message}",
                              "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Обновляем данные при загрузке страницы
                LoadCategories();
                LoadProducts();
                UpdateProductsCount();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке: {ex.Message}",
                              "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadCategories()
        {
            try
            {
                if (AppConnect.modelOdb == null) return;

                var categories = AppConnect.modelOdb.Categories?
                    .OrderBy(c => c.Name)
                    .ToList() ?? new List<Categories>();

                if (cmbCategory != null)
                {
                    cmbCategory.ItemsSource = categories;
                    cmbCategory.DisplayMemberPath = "Name";
                    cmbCategory.SelectedValuePath = "Id";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки категорий: {ex.Message}");
            }
        }

        private void LoadProducts()
        {
            try
            {
                if (AppConnect.modelOdb == null) return;

                var products = AppConnect.modelOdb.Products?
                    .OrderBy(p => p.Name)
                    .ToList() ?? new List<Products>();

                if (dgProducts != null)
                {
                    dgProducts.ItemsSource = products;
                }

                UpdateProductsCount();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки товаров: {ex.Message}");
            }
        }

        private void UpdateProductsCount()
        {
            try
            {
                if (AppConnect.modelOdb?.Products != null && txtProductsCount != null)
                {
                    int count = AppConnect.modelOdb.Products.Count();
                    txtProductsCount.Text = count.ToString();
                }
            }
            catch
            {
                // Игнорируем ошибки
            }
        }

        private void ApplyFilter()
        {
            try
            {
                if (AppConnect.modelOdb?.Products == null || cmbTypeFilter == null)
                    return;

                var query = AppConnect.modelOdb.Products.AsQueryable();

                // Фильтр по типу
                if (cmbTypeFilter.SelectedItem is ComboBoxItem typeItem && typeItem != null)
                {
                    string typeFilter = typeItem.Content?.ToString();
                    if (!string.IsNullOrEmpty(typeFilter) && typeFilter != "Все типы")
                    {
                        query = query.Where(p => p.Type == typeFilter);
                    }
                }

                if (dgProducts != null)
                {
                    dgProducts.ItemsSource = query.OrderBy(p => p.Name).ToList();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при фильтрации: {ex.Message}");
            }
        }

        private void cmbTypeFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void dgProducts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (dgProducts?.SelectedItem is Products product && product != null)
                {
                    _selectedProduct = product;
                    _isEditMode = true;
                    LoadProductToForm(product);

                    if (txtFormTitle != null)
                        txtFormTitle.Text = "Редактирование товара";

                    if (btnDelete != null)
                        btnDelete.Visibility = Visibility.Visible;

                    if (borderSelectedInfo != null && txtSelectedInfo != null)
                    {
                        borderSelectedInfo.Visibility = Visibility.Visible;
                        txtSelectedInfo.Text = $"Выбран: {product.Name} (ID: {product.Id})";
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при выборе товара: {ex.Message}");
            }
        }

        private void LoadProductToForm(Products product)
        {
            try
            {
                if (product == null) return;

                if (txtName != null)
                    txtName.Text = product.Name ?? "";

                if (txtDescription != null)
                    txtDescription.Text = product.Description ?? "";

                if (txtPrice != null)
                    txtPrice.Text = product.Price.ToString("F0");

                if (cmbType != null && cmbType.Items != null)
                {
                    foreach (ComboBoxItem item in cmbType.Items)
                    {
                        if (item?.Content?.ToString() == product.Type)
                        {
                            cmbType.SelectedItem = item;
                            break;
                        }
                    }
                }

                if (cmbCategory != null)
                {
                    cmbCategory.SelectedValue = product.CategoryId;
                }

                if (txtImage != null)
                    txtImage.Text = product.ImageUrl ?? "";

                _selectedImagePath = product.ImageUrl;

                if (chkActive != null)
                    chkActive.IsChecked = product.IsActive;

                LoadImagePreview(product.ImageUrl);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при загрузке данных товара: {ex.Message}");
            }
        }

        private void LoadImagePreview(string imageUrl)
        {
            try
            {
                if (imgPreview == null || txtNoImage == null) return;

                if (string.IsNullOrEmpty(imageUrl))
                {
                    imgPreview.Source = null;
                    txtNoImage.Visibility = Visibility.Visible;
                    return;
                }

                string fileName = System.IO.Path.GetFileName(imageUrl);
                string resourcesPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "products");
                string fullPath = System.IO.Path.Combine(resourcesPath, fileName);

                if (System.IO.File.Exists(fullPath))
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(fullPath);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    imgPreview.Source = bitmap;
                    txtNoImage.Visibility = Visibility.Collapsed;
                }
                else
                {
                    imgPreview.Source = null;
                    txtNoImage.Visibility = Visibility.Visible;
                }
            }
            catch
            {
                if (imgPreview != null) imgPreview.Source = null;
                if (txtNoImage != null) txtNoImage.Visibility = Visibility.Visible;
            }
        }

        private void btnAddNew_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _selectedProduct = null;
                _isEditMode = false;
                ClearForm();

                if (txtFormTitle != null)
                    txtFormTitle.Text = "Добавление нового товара";

                if (btnDelete != null)
                    btnDelete.Visibility = Visibility.Collapsed;

                if (borderSelectedInfo != null)
                    borderSelectedInfo.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка: {ex.Message}");
            }
        }

        private void ClearForm()
        {
            try
            {
                if (txtName != null) txtName.Clear();
                if (txtDescription != null) txtDescription.Clear();
                if (txtPrice != null) txtPrice.Clear();
                if (cmbType != null) cmbType.SelectedIndex = 0;
                if (cmbCategory != null) cmbCategory.SelectedIndex = -1;
                if (txtImage != null) txtImage.Clear();

                _selectedImagePath = null;

                if (imgPreview != null) imgPreview.Source = null;
                if (txtNoImage != null) txtNoImage.Visibility = Visibility.Visible;
                if (chkActive != null) chkActive.IsChecked = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при очистке формы: {ex.Message}");
            }
        }

        private void btnSelectImage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog dialog = new OpenFileDialog
                {
                    Filter = "Изображения (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png",
                    Title = "Выберите изображение для товара"
                };

                if (dialog.ShowDialog() == true)
                {
                    string fileName = System.IO.Path.GetFileName(dialog.FileName);
                    string resourcesPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "products");

                    if (!System.IO.Directory.Exists(resourcesPath))
                    {
                        System.IO.Directory.CreateDirectory(resourcesPath);
                    }

                    string destPath = System.IO.Path.Combine(resourcesPath, fileName);
                    System.IO.File.Copy(dialog.FileName, destPath, true);

                    _selectedImagePath = $"/products/{fileName}";
                    if (txtImage != null)
                        txtImage.Text = _selectedImagePath;

                    LoadImagePreview(_selectedImagePath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при копировании изображения: {ex.Message}",
                              "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool ValidateForm()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtName?.Text))
                {
                    MessageBox.Show("Введите название товара", "Ошибка",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtName?.Focus();
                    return false;
                }

                if (string.IsNullOrWhiteSpace(txtPrice?.Text))
                {
                    MessageBox.Show("Введите цену товара", "Ошибка",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtPrice?.Focus();
                    return false;
                }

                if (!decimal.TryParse(txtPrice.Text, out decimal price) || price < 0)
                {
                    MessageBox.Show("Введите корректную цену (положительное число)", "Ошибка",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtPrice?.Focus();
                    return false;
                }

                if (cmbType?.SelectedItem == null)
                {
                    MessageBox.Show("Выберите тип товара", "Ошибка",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }

                if (cmbCategory?.SelectedItem == null)
                {
                    MessageBox.Show("Выберите категорию", "Ошибка",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка валидации: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ValidateForm())
                    return;

                if (AppConnect.modelOdb == null)
                {
                    MessageBox.Show("Нет подключения к базе данных", "Ошибка",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                string type = (cmbType?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Товар";

                if (cmbCategory?.SelectedValue == null)
                {
                    MessageBox.Show("Ошибка: не выбрана категория", "Ошибка",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                int categoryId = (int)cmbCategory.SelectedValue;
                decimal price = decimal.Parse(txtPrice.Text);

                if (_selectedProduct == null)
                {
                    // Создание нового товара
                    var newProduct = new Products
                    {
                        Name = txtName.Text.Trim(),
                        Description = string.IsNullOrWhiteSpace(txtDescription?.Text) ? null : txtDescription.Text.Trim(),
                        Price = price,
                        Type = type,
                        CategoryId = categoryId,
                        ImageUrl = _selectedImagePath,
                        IsActive = chkActive?.IsChecked ?? true
                    };

                    AppConnect.modelOdb.Products.Add(newProduct);
                    AppConnect.modelOdb.SaveChanges();

                    // Добавляем запись на склад для товаров
                    if (type == "Товар")
                    {
                        var stock = new Stock
                        {
                            ProductId = newProduct.Id,
                            Quantity = 0
                        };
                        AppConnect.modelOdb.Stock.Add(stock);
                        AppConnect.modelOdb.SaveChanges();

                        MessageBox.Show($"Товар «{newProduct.Name}» успешно добавлен и появится на складе после обновления страницы склада",
                                      "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show($"Услуга «{newProduct.Name}» успешно добавлена",
                                      "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    }

                    // Вызываем событие обновления статистики
                    AppEvents.OnStatisticsChanged();
                }
                else
                {
                    // Обновление существующего товара
                    _selectedProduct.Name = txtName.Text.Trim();
                    _selectedProduct.Description = string.IsNullOrWhiteSpace(txtDescription?.Text) ? null : txtDescription.Text.Trim();
                    _selectedProduct.Price = price;
                    _selectedProduct.Type = type;
                    _selectedProduct.CategoryId = categoryId;
                    _selectedProduct.ImageUrl = _selectedImagePath;
                    _selectedProduct.IsActive = chkActive?.IsChecked ?? true;

                    AppConnect.modelOdb.SaveChanges();

                    MessageBox.Show($"Товар «{_selectedProduct.Name}» успешно обновлен",
                                  "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                    AppEvents.OnStatisticsChanged();
                }

                // Обновляем список товаров
                LoadProducts();

                // Очищаем форму если это было добавление
                if (_selectedProduct == null)
                {
                    ClearForm();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_selectedProduct == null) return;

                if (AppConnect.modelOdb == null)
                {
                    MessageBox.Show("Нет подключения к базе данных", "Ошибка",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                bool inOrders = AppConnect.modelOdb.OrderItems?.Any(oi => oi.ProductId == _selectedProduct.Id) ?? false;
                bool hasStock = AppConnect.modelOdb.Stock?.Any(s => s.ProductId == _selectedProduct.Id && s.Quantity > 0) ?? false;

                if (inOrders || hasStock)
                {
                    string reason = inOrders ? "присутствует в заказах" : "имеет остатки на складе";
                    MessageBox.Show($"Нельзя удалить товар, который {reason}!",
                                  "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var result = MessageBox.Show(
                    $"Вы действительно хотите удалить товар «{_selectedProduct.Name}»?",
                    "Подтверждение удаления",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    var stock = AppConnect.modelOdb.Stock?.FirstOrDefault(s => s.ProductId == _selectedProduct.Id);
                    if (stock != null)
                    {
                        AppConnect.modelOdb.Stock.Remove(stock);
                    }

                    AppConnect.modelOdb.Products.Remove(_selectedProduct);
                    AppConnect.modelOdb.SaveChanges();

                    MessageBox.Show($"Товар «{_selectedProduct.Name}» удален", "Успех",
                                  MessageBoxButton.OK, MessageBoxImage.Information);

                    _selectedProduct = null;
                    _isEditMode = false;
                    ClearForm();
                    LoadProducts();

                    AppEvents.OnStatisticsChanged();

                    if (btnDelete != null)
                        btnDelete.Visibility = Visibility.Collapsed;

                    if (borderSelectedInfo != null)
                        borderSelectedInfo.Visibility = Visibility.Collapsed;

                    if (txtFormTitle != null)
                        txtFormTitle.Text = "Добавление нового товара";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_isEditMode)
                {
                    var result = MessageBox.Show("Отменить изменения?", "Подтверждение",
                                               MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result == MessageBoxResult.Yes)
                    {
                        _selectedProduct = null;
                        _isEditMode = false;
                        ClearForm();

                        if (btnDelete != null)
                            btnDelete.Visibility = Visibility.Collapsed;

                        if (borderSelectedInfo != null)
                            borderSelectedInfo.Visibility = Visibility.Collapsed;

                        if (txtFormTitle != null)
                            txtFormTitle.Text = "Добавление нового товара";
                    }
                }
                else
                {
                    ClearForm();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка: {ex.Message}");
            }
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (AppFrame.FrameMain.CanGoBack)
                {
                    AppFrame.FrameMain.GoBack();
                }
                else
                {
                    AppFrame.FrameMain.Navigate(new Pages.Admin.AdminDashboard.AdminDashboardPage());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка навигации: {ex.Message}",
                              "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void txtPrice_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !char.IsDigit(e.Text, 0);
        }

        private void FormField_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Пустой обработчик
        }

        private void FormField_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Пустой обработчик
        }
    }
}