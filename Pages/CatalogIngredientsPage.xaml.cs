using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
//using TrackSync.DataBase.Model;
using TrackSync.DataBase.WorkWithDB;
using TrackSync.Interface;
using TrackSync.Windows;

namespace TrackSync.Pages
{
    public partial class CatalogIngredientsPage : Page
    {
        // Вспомогательный класс для отслеживания выбора
        public class IngredientWithSelection
        {
            public Ingredient Ingredient { get; set; }
            public bool IsSelected { get; set; }
        }

        private List<IngredientWithSelection> _ingredientItems;
        private List<IngredientWithSelection> _filteredItems;
        private readonly IRecipeIngredientCallback _callback;

        public CatalogIngredientsPage(IRecipeIngredientCallback callback)
        {
            InitializeComponent();
            _callback = callback;
            LoadIngredients();
        }

        /// <summary>
        /// Загрузка всех ингредиентов из базы данных
        /// </summary>
        private void LoadIngredients()
        {
            var ingredientList = TrackSyncManagerList.GetAllIngredients();

            if (ingredientList != null)
            {
                _ingredientItems = ingredientList
                    .ConvertAll(i => new IngredientWithSelection
                    {
                        Ingredient = i,
                        IsSelected = false
                    });

                _filteredItems = new List<IngredientWithSelection>(_ingredientItems);
                IngredientsListBox.ItemsSource = _filteredItems;
            }
        }

        /// <summary>
        /// Обработка поиска
        /// </summary>
        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_ingredientItems == null)
                return;

            string searchText = SearchTextBox.Text?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(searchText))
            {
                _filteredItems = new List<IngredientWithSelection>(_ingredientItems);
            }
            else
            {
                _filteredItems = _ingredientItems
                    .FindAll(i => i.Ingredient.Title.ToLower().Contains(searchText.ToLower()));
            }

            IngredientsListBox.ItemsSource = _filteredItems;
            ErrorMessageHide(); // Скрываем ошибку при поиске
        }

        /// <summary>
        /// Обработка клика по элементу ингредиента
        /// </summary>
        private void IngredientItem_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is IngredientWithSelection item)
            {
                // Переключаем состояние выбора
                item.IsSelected = !item.IsSelected;
                IngredientsListBox.Items.Refresh();
                ErrorMessageHide(); // Скрываем ошибку при выборе
            }
        }

        private void CreateIngredientButton_Click(object sender, RoutedEventArgs e)
        {
            FrameContext.MainWindowFrame.Navigate(new CreateIngredientPage());
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (FrameContext.MainWindowFrame.CanGoBack)
                FrameContext.MainWindowFrame.GoBack();
        }

        // Новый метод для кнопки "Добавить выбранное"
        private void AddSelectedButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorMessageHide(); // Скрываем ошибку перед проверкой

            // Получаем выбранные ингредиенты
            var selectedIngredients = _filteredItems
                .Where(i => i.IsSelected)
                .Select(i => i.Ingredient)
                .ToList();

            if (selectedIngredients.Count == 0)
            {
                ErrorMessageVisible("*Пожалуйста, выберите хотя бы один ингредиент");
                return;
            }

            // Вызываем коллбэк для передачи ингредиентов обратно
            _callback?.OnIngredientsSelected(selectedIngredients);

            // Возвращаемся назад
            if (FrameContext.MainWindowFrame.CanGoBack)
                FrameContext.MainWindowFrame.GoBack();
        }

        // Методы для работы с сообщением об ошибке 
        private void ErrorMessageVisible(string message)
        {
            if (ErrorMessage != null)
            {
                ErrorMessage.Visibility = Visibility.Visible;
                ErrorMessage.Text = message;
            }
        }

        private void ErrorMessageHide()
        {
            if (ErrorMessage != null)
            {
                ErrorMessage.Visibility = Visibility.Collapsed;
            }
        }
    }
}