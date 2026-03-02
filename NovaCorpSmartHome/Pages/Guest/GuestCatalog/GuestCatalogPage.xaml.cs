using NovaCorpSmartHome.ApplicationData;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
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

namespace NovaCorpSmartHome.Pages.Guest.GuestCatalog
{
    /// <summary>
    /// Логика взаимодействия для GuestCatalogPage.xaml
    /// </summary>
    public class BreadcrumbItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public partial class GuestCatalogPage : Page
    {
        private ObservableCollection<Products> products = new ObservableCollection<Products>();
        private ObservableCollection<BreadcrumbItem> breadcrumbs = new ObservableCollection<BreadcrumbItem>();
        private List<Categories> allCategories;
        private bool isLoaded = false;

        public GuestCatalogPage()
        {
            InitializeComponent();
            icProducts.ItemsSource = products;
            icBreadcrumbs.ItemsSource = breadcrumbs;

            Loaded += Page_Loaded;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (!isLoaded)
            {
                LoadCategories();
                LoadAllProducts();
                isLoaded = true;
            }
        }

        private void LoadCategories()
        {
            try
            {
                tvCategories.Items.Clear();
                allCategories = AppConnect.modelOdb.Categories.ToList();

                // Загружаем только корневые категории
                var rootCats = allCategories.Where(c => c.ParentId == null).ToList();

                foreach (var root in rootCats)
                {
                    var item = CreateTreeViewItem(root);
                    tvCategories.Items.Add(item);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки категорий: " + ex.Message);
            }
        }

        private TreeViewItem CreateTreeViewItem(Categories category)
        {
            var item = new TreeViewItem();
            item.Header = category.Name;
            item.Tag = category;

            var children = allCategories.Where(c => c.ParentId == category.Id).ToList();
            foreach (var child in children)
            {
                item.Items.Add(CreateTreeViewItem(child));
            }

            return item;
        }

        private void ProcessProductImages(List<Products> productsList)
        {
            string basePath = System.AppDomain.CurrentDomain.BaseDirectory;
            string resourcesPath = System.IO.Path.Combine(basePath, "Resources", "products");

            // Создаем папку, если её нет
            if (!System.IO.Directory.Exists(resourcesPath))
                System.IO.Directory.CreateDirectory(resourcesPath);

            foreach (var p in productsList)
            {
                if (!string.IsNullOrEmpty(p.ImageUrl))
                {
                    // Извлекаем только имя файла - используем System.IO.Path
                    string fileName = System.IO.Path.GetFileName(p.ImageUrl);

                    // Путь к файлу в папке приложения
                    string fullPath = System.IO.Path.Combine(resourcesPath, fileName);

                    // Проверяем, существует ли файл
                    if (System.IO.File.Exists(fullPath))
                    {
                        // Используем абсолютный путь к файлу
                        p.ImageUrl = fullPath;
                    }
                    else
                    {
                        // Пробуем найти в ресурсах сборки
                        p.ImageUrl = $"pack://application:,,,/Resources/products/{fileName}";
                    }
                }
                else
                {
                    string defaultImage = System.IO.Path.Combine(basePath, "Resources", "no-image.png");
                    if (System.IO.File.Exists(defaultImage))
                        p.ImageUrl = defaultImage;
                    else
                        p.ImageUrl = "pack://application:,,,/Resources/no-image.png";
                }
            }
        }

        private List<Categories> GetCategoryPath(int categoryId)
        {
            var path = new List<Categories>();
            var current = allCategories?.FirstOrDefault(c => c.Id == categoryId);

            while (current != null)
            {
                path.Insert(0, current);
                current = allCategories?.FirstOrDefault(c => c.Id == current.ParentId);
            }

            return path;
        }

        private void UpdateBreadcrumbs(List<Categories> categoryPath)
        {
            breadcrumbs.Clear();
            if (categoryPath != null)
            {
                foreach (var cat in categoryPath)
                {
                    breadcrumbs.Add(new BreadcrumbItem { Id = cat.Id, Name = cat.Name });
                }
            }
        }

        private void LoadAllProducts()
        {
            try
            {
                products.Clear();
                breadcrumbs.Clear();

                var allProducts = AppConnect.modelOdb.Products
                    .Where(p => p.IsActive == true)
                    .ToList();

                ProcessProductImages(allProducts);

                foreach (var p in allProducts)
                    products.Add(p);

                txtProductsCount.Text = products.Count + " товаров";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки товаров: " + ex.Message);
            }
        }

        private void LoadProductsByCategory(int categoryId)
        {
            try
            {
                products.Clear();

                // Получаем все ID подкатегорий
                var categoryIds = GetAllCategoryIds(categoryId).ToList();
                categoryIds.Add(categoryId);

                var catProducts = AppConnect.modelOdb.Products
                    .Where(p => p.IsActive == true && categoryIds.Contains(p.CategoryId))
                    .ToList();

                ProcessProductImages(catProducts);

                foreach (var p in catProducts)
                    products.Add(p);

                txtProductsCount.Text = products.Count + " товаров";

                // Обновляем хлебные крошки
                var path = GetCategoryPath(categoryId);
                UpdateBreadcrumbs(path);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки товаров: " + ex.Message);
            }
        }

        private IEnumerable<int> GetAllCategoryIds(int parentId)
        {
            if (allCategories == null) yield break;

            var children = allCategories.Where(c => c.ParentId == parentId).Select(c => c.Id);
            foreach (var childId in children)
            {
                yield return childId;
                foreach (var subChildId in GetAllCategoryIds(childId))
                    yield return subChildId;
            }
        }

        private void tvCategories_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (tvCategories.SelectedItem is TreeViewItem selected && selected.Tag is Categories cat)
            {
                LoadProductsByCategory(cat.Id);
            }
        }

        private void Breadcrumb_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is TextBlock textBlock && textBlock.Tag is int categoryId)
            {
                var category = allCategories?.FirstOrDefault(c => c.Id == categoryId);
                if (category != null)
                {
                    // Находим и выделяем соответствующий элемент в TreeView
                    SelectTreeViewItem(tvCategories.Items, categoryId);
                    LoadProductsByCategory(categoryId);
                }
            }
        }

        private bool SelectTreeViewItem(ItemCollection items, int categoryId)
        {
            foreach (var item in items)
            {
                if (item is TreeViewItem treeItem)
                {
                    if (treeItem.Tag is Categories cat && cat.Id == categoryId)
                    {
                        treeItem.IsSelected = true;
                        treeItem.BringIntoView();
                        return true;
                    }

                    if (SelectTreeViewItem(treeItem.Items, categoryId))
                        return true;
                }
            }
            return false;
        }

        private void Home_Click(object sender, MouseButtonEventArgs e)
        {
            // Снимаем выделение со всех элементов
            foreach (var item in tvCategories.Items)
            {
                if (item is TreeViewItem treeItem)
                    treeItem.IsSelected = false;
            }

            LoadAllProducts();
        }

        private void btnAuthorization_Click(object sender, RoutedEventArgs e)
        {
            AppFrame.FrameMain.Navigate(new Pages.Authorization.AuthorizationPage());
        }

        private void btnClearSearch_Click(object sender, RoutedEventArgs e)
        {
            txtSearch.Text = "";
            btnClearSearch.Visibility = Visibility.Collapsed;

            if (tvCategories.SelectedItem is TreeViewItem selected && selected.Tag is Categories cat)
            {
                LoadProductsByCategory(cat.Id);
            }
            else
            {
                LoadAllProducts();
            }
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            string search = txtSearch.Text.ToLower();
            btnClearSearch.Visibility = string.IsNullOrEmpty(search) ?
                Visibility.Collapsed : Visibility.Visible;

            if (string.IsNullOrWhiteSpace(search))
            {
                if (tvCategories.SelectedItem is TreeViewItem selected && selected.Tag is Categories cat)
                {
                    LoadProductsByCategory(cat.Id);
                }
                else
                {
                    LoadAllProducts();
                }
                return;
            }

            try
            {
                products.Clear();

                var found = AppConnect.modelOdb.Products
                    .Where(p => p.IsActive == true && p.Name.ToLower().Contains(search))
                    .ToList();

                ProcessProductImages(found);

                foreach (var p in found)
                    products.Add(p);

                txtProductsCount.Text = products.Count + " товаров";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка поиска: " + ex.Message);
            }
        }
        private void ProductCard_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var product = (sender as FrameworkElement)?.DataContext as Products;
            if (product != null)
            {
                AppFrame.FrameMain.Navigate(new GuestProductDetails.GuestProductDetailsPage(product));
            }
        }
        private void cmbSort_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbSort.SelectedItem == null || products.Count == 0) return;

            string sortBy = ((ComboBoxItem)cmbSort.SelectedItem).Content.ToString();

            var sortedList = products.ToList();

            switch (sortBy)
            {
                case "Сначала дешевле":
                    sortedList = sortedList.OrderBy(p => p.Price).ToList();
                    break;
                case "Сначала дороже":
                    sortedList = sortedList.OrderByDescending(p => p.Price).ToList();
                    break;
                case "По названию":
                    sortedList = sortedList.OrderBy(p => p.Name).ToList();
                    break;
            }

            products.Clear();
            foreach (var p in sortedList)
                products.Add(p);
        }
    }
}