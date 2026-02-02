using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
//using TrackSync.DataBase.Model;
using TrackSync.DataBase.WorkWithDB;

namespace TrackSync.Pages
{
    public partial class CatalogRecipesPage : Page
    {
        // Вспомогательный класс для отображения рецепта с расчётными данными
        public class RecipeDisplayItem
        {
            public Recipe Recipe { get; set; }
            public bool IsSelected { get; set; }
            public int Weight { get; set; } // Общий вес рецепта
            public int Calories { get; set; } // Общая калорийность
        }

        private List<RecipeDisplayItem> _recipeItems;
        private List<RecipeDisplayItem> _filteredItems;
        private RecipeDisplayItem _selectedRecipe;

        public CatalogRecipesPage()
        {
            InitializeComponent();
            LoadRecipes();
        }

        /// <summary>
        /// Загрузка всех рецептов из базы данных
        /// </summary>
        private void LoadRecipes()
        {
            var recipeList = TrackSyncManagerList.GetAllRecipes();

            if (recipeList != null && recipeList.Any())
            {
                _recipeItems = recipeList
                    .Select(r => new RecipeDisplayItem
                    {
                        Recipe = r,
                        IsSelected = false,
                        Weight = TrackSyncManagerList.CalculateRecipeWeight(r),
                        Calories = TrackSyncManagerList.CalculateRecipeCalories(r)
                    })
                    .ToList();

                _filteredItems = new List<RecipeDisplayItem>(_recipeItems);
                RecipesListBox.ItemsSource = _filteredItems;
            }
            else
            {
                // Показываем сообщение "Рецепты не найдены"
                var emptyList = new List<RecipeDisplayItem>();
                RecipesListBox.ItemsSource = emptyList;

                MessageBox.Show("Рецепты не найдены. Создайте первый рецепт!", "Информация",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// Обработка поиска
        /// </summary>
        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_recipeItems == null)
                return;

            string searchText = SearchTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(searchText))
            {
                _filteredItems = new List<RecipeDisplayItem>(_recipeItems);
            }
            else
            {
                _filteredItems = _recipeItems
                    .Where(r => r.Recipe.Title.ToLower().Contains(searchText.ToLower()))
                    .ToList();
            }

            // Снимаем выбор при поиске
            DeselectAll();

            RecipesListBox.ItemsSource = _filteredItems;
        }

        /// <summary>
        /// Обработка клика по элементу рецепта (одиночный выбор)
        /// </summary>
        private void RecipeItem_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is RecipeDisplayItem item)
            {
                // Снимаем выбор со всех
                DeselectAll();

                // Выбираем новый
                item.IsSelected = true;
                _selectedRecipe = item;

                // Обновляем интерфейс
                RecipesListBox.Items.Refresh();
            }
        }

        /// <summary>
        /// Снять выбор со всех рецептов
        /// </summary>
        private void DeselectAll()
        {
            if (_recipeItems != null)
            {
                foreach (var item in _recipeItems)
                {
                    item.IsSelected = false;
                }
            }
            _selectedRecipe = null;
        }

        /// <summary>
        /// Получить выбранный рецепт
        /// </summary>
        public Recipe GetSelectedRecipe()
        {
            return _selectedRecipe?.Recipe;
        }

        private void CreateRecipeButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new CreateRecipePage());
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService.CanGoBack)
                NavigationService.GoBack();
        }
    }
}