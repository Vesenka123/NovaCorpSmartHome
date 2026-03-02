using NovaCorpSmartHome.ApplicationData;
using NovaCorpSmartHome.Pages.Guest.GuestCatalog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace NovaCorpSmartHome.Pages.Guest.GuestProductDetails
{
    public partial class GuestProductDetailsPage : Page
    {
        private Products _selectedProduct;

        public GuestProductDetailsPage(Products product)
        {
            InitializeComponent();
            _selectedProduct = product ?? throw new ArgumentNullException(nameof(product));
            LoadProductData();
        }

        private void LoadProductData()
        {
            // Название
            txtName.Text = _selectedProduct.Name;

            // Тип: "Товар" или "Услуга"
            txtType.Text = _selectedProduct.Type == "Товар" ? "Физическое устройство" : "Услуга";

            // Цена
            txtPrice.Text = $"{_selectedProduct.Price:N0} ₽";

            // Описание
            txtDescription.Text = !string.IsNullOrEmpty(_selectedProduct.Description)
                ? _selectedProduct.Description
                : "Описание отсутствует.";

            // Загружаем изображение (только для товаров)
            if (_selectedProduct.Type == "Товар")
            {
                LoadProductImage();
            }
            else
            {
                // Для услуг показываем заглушку
                LoadDefaultImage();
            }
        }

        private void LoadProductImage()
        {
            try
            {
                string basePath = AppDomain.CurrentDomain.BaseDirectory;
                string resourcesPath = Path.Combine(basePath, "Resources", "products");

                if (!string.IsNullOrEmpty(_selectedProduct.ImageUrl))
                {
                    // Извлекаем только имя файла
                    string fileName = Path.GetFileName(_selectedProduct.ImageUrl);

                    // Путь к файлу в папке приложения
                    string fullPath = Path.Combine(resourcesPath, fileName);

                    // Проверяем, существует ли файл
                    if (File.Exists(fullPath))
                    {
                        // Используем абсолютный путь к файлу
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.UriSource = new Uri(fullPath, UriKind.Absolute);
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();
                        imgProduct.Source = bitmap;
                    }
                    else
                    {
                        // Пробуем найти в ресурсах сборки
                        string resourcePath = $"pack://application:,,,/Resources/products/{fileName}";
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.UriSource = new Uri(resourcePath, UriKind.Absolute);
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();
                        imgProduct.Source = bitmap;
                    }
                }
                else
                {
                    LoadDefaultImage();
                }
            }
            catch (Exception)
            {
                LoadDefaultImage();
            }
        }

        private void LoadDefaultImage()
        {
            try
            {
                string basePath = AppDomain.CurrentDomain.BaseDirectory;
                string defaultImage = Path.Combine(basePath, "Resources", "no-image.png");

                if (File.Exists(defaultImage))
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(defaultImage, UriKind.Absolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    imgProduct.Source = bitmap;
                }
                else
                {
                    // Пробуем как ресурс
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri("pack://application:,,,/Resources/no-image.png", UriKind.Absolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    imgProduct.Source = bitmap;
                }
            }
            catch
            {
                imgProduct.Source = null;
            }
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            AppFrame.FrameMain.Navigate(new GuestCatalogPage());
        }
    }
}