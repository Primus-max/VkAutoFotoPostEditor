using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace FotoPostEditor
{
    public partial class MainWindow : Window
    {
        private List<string> _imagePaths = new List<string>();
        private int _currentIndex = 0;
        private int curCountImage;

        public MainWindow()
        {
            InitializeComponent();

            this.KeyDown += MainWindow_KeyDown;

            // Загрузка путей к изображениям из директории
            var imageDirectory = @"C:\Users\VKauto\net6.0-windows\tdlib\photos";
            _imagePaths = Directory.GetFiles(imageDirectory, "*.jpg").ToList();
            curCountImage = _imagePaths.Count;

            if (_imagePaths.Count > 0)
            {
                // Отображение первого изображения
                ShowImage(_imagePaths[_currentIndex]);
            }
            else
            {
                // Если в директории нет изображений, скрываем кнопки просмотра и удаления
                RecognizeButton.Visibility = Visibility.Collapsed;
                DeleteButton.Visibility = Visibility.Collapsed;
            }
        }


        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            // Проверяем, была ли нажата клавиша Enter
            if (e.Key == Key.Enter)
            {
                // Вызываем обработчик событий Click для кнопки RecognizeButton
                RecognizeButton_Click(RecognizeButton, null);
            }
            // Проверяем, была ли нажата клавиша пробел
            if (e.Key == Key.Space)
            {
                // Вызываем обработчик событий Click для кнопки DeleteButton
                DeleteButton_Click(DeleteButton, null);
            }
        }

        private void RecognizeButton_Click(object sender, RoutedEventArgs e)
        {

            if (_currentIndex >= _imagePaths.Count)
            {
                _currentIndex = 0;
            }
            var oldImagePath = _imagePaths[_currentIndex];
            var newImagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ForPosting", Path.GetFileName(oldImagePath));
            var postingDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ForPosting");
            if (!Directory.Exists(postingDirectory))
            {
                Directory.CreateDirectory(postingDirectory);
            }
            try
            {
                File.Move(oldImagePath, newImagePath, true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Файл не найден: {ex.Message}");
            }

            _imagePaths[_currentIndex] = newImagePath;
            curCountImage--;

            // Обновляем информацию в файле
            var jsonDirectoryPath = @"C:\Users\VKauto\net6.0-windows\content";

            // получаем список json-файлов в директории
            var jsonFiles = Directory.GetFiles(jsonDirectoryPath, "*.json"); ;
            foreach (var jsonFilePath in jsonFiles)
            {
                var jsonString = File.ReadAllText(jsonFilePath);
                var contentList = JsonConvert.DeserializeObject<List<Content>>(jsonString);
                var contentToUpdate = contentList.FirstOrDefault(c => c.ImagePaths.Contains(oldImagePath));
                if (contentToUpdate != null)
                {
                    var updatedImagePaths = contentToUpdate?.ImagePaths?.Select(ip => ip.Replace(oldImagePath, newImagePath)).ToList();
                    contentToUpdate.ImagePaths = updatedImagePaths;
                    contentToUpdate.IsForPosting = true;
                    var updatedJsonString = JsonConvert.SerializeObject(contentList);
                    File.WriteAllText(jsonFilePath, updatedJsonString);
                    break;
                }
            }


            try
            {
                _currentIndex++;
                ShowImage(_imagePaths[_currentIndex]);
            }
            catch (Exception)
            {
                MessageBox.Show("Фотки закончились");
                Process.GetCurrentProcess().Kill();
            }


        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (_imagePaths.Count == 0) return;

            // Удаление текущего изображения
            var currentImageFilePath = _imagePaths[_currentIndex];
            File.Delete(currentImageFilePath);
            _imagePaths.RemoveAt(_currentIndex);

            // Обновление JSON файла
            UpdateJsonFile(currentImageFilePath);

            // Отображение следующего изображения, если оно есть
            if (_imagePaths.Count > 0)
            {
                _currentIndex %= _imagePaths.Count;
                ShowImage(_imagePaths[_currentIndex]);
            }
            else
            {
                // Если удалили все изображения, скрываем кнопки просмотра и удаления
                RecognizeButton.Visibility = Visibility.Collapsed;
                DeleteButton.Visibility = Visibility.Collapsed;
            }
        }


        private void ShowImage(string imagePath)
        {
            if (curCountImage == 0)
            {
                MessageBox.Show("Фоточки закончились!");
                return;
            }

            using (FileStream stream = new FileStream(imagePath, FileMode.Open, FileAccess.ReadWrite))
            {
                // Загружаем изображение в ImageViewer
                BitmapImage image = new BitmapImage();
                image.BeginInit();
                image.StreamSource = stream;
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.EndInit();
                ImageViewer.Source = image;
            }
        }

        private static void UpdateJsonFile(string currentImageFilePath)
        {
            // путь к директории, где находятся json-файлы
            var jsonDirectoryPath = @"C:\Users\VKauto\net6.0-windows\content";

            // получаем список json-файлов в директории
            var jsonFiles = Directory.GetFiles(jsonDirectoryPath, "*.json");

            // перебираем файлы и удаляем запись об удаленной фотографии
            foreach (var jsonFilePath in jsonFiles)
            {
                var jsonString = File.ReadAllText(jsonFilePath);
                var contentList = JsonConvert.DeserializeObject<List<Content>>(jsonString);
                var contentToRemove = contentList.FirstOrDefault(c => c.ImagePaths.Contains(currentImageFilePath));
                if (contentToRemove != null)
                {
                    var updatedImagePaths = contentToRemove?.ImagePaths?.Where(ip => ip != currentImageFilePath).ToList();
                    contentToRemove.ImagePaths = updatedImagePaths;
                    var updatedJsonString = JsonConvert.SerializeObject(contentList);
                    File.WriteAllText(jsonFilePath, updatedJsonString);
                    break; // выходим из цикла, если запись удалена из файла
                }
            }
        }


    }
}
