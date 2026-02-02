using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TrackSync.DataBase;
using TrackSync.DataBase.WorkWithDB;
using TrackSync.Interface;
using TrackSync.Windows;

namespace TrackSync.Pages
{
    public partial class CatalogRecipesPage : Page
    {
        // Вспомогательный класс для отслеживания выбора
        public class RecipeWithSelection
        {
            public Recipe Recipe { get; set; }
            public bool IsSelected { get; set; }

            public bool IsUserRecipe
            {
                get
                {
                    return Recipe.CreatorUserId == SessionManager.CurrentUserId;
                }
            }

            public System.Windows.Visibility ActionButtonsVisibility
            {
                get
                {
                    return IsUserRecipe ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
                }
            }
        }

        private List<RecipeWithSelection> _recipeItems;
        private List<RecipeWithSelection> _filteredItems;
        private readonly IRecipeCallback _callback;
        private bool _showOnlyMyItems = false;

        public CatalogRecipesPage(IRecipeCallback callback = null)
        {
            InitializeComponent();
            _callback = callback;
            LoadRecipes();
            UpdateFilterButtons();
        }

        /// <summary>
        /// Загрузка рецептов из базы данных с учётом фильтрации
        /// </summary>
        private void LoadRecipes()
        {
            List<Recipe> recipeList;

            if (_showOnlyMyItems)
            {
                // Показываем только рецепты, созданные текущим пользователем
                recipeList = TrackSyncManagerList.GetUserRecipes(SessionManager.CurrentUserId);
            }
            else
            {
                // Показываем все рецепты (системные + созданные пользователем)
                recipeList = TrackSyncManagerList.GetAllRecipesForUser(SessionManager.CurrentUserId);
            }

            if (recipeList != null)
            {
                _recipeItems = recipeList
                    .ConvertAll(r => new RecipeWithSelection
                    {
                        Recipe = r,
                        IsSelected = false
                    });

                _filteredItems = new List<RecipeWithSelection>(_recipeItems);
                RecipesListBox.ItemsSource = _filteredItems;
            }
            else
            {
                RecipesListBox.ItemsSource = new List<RecipeWithSelection>();
            }
        }

        /// <summary>
        /// Обработка клика по кнопке "Все"
        /// </summary>
        private void AllButton_Click(object sender, RoutedEventArgs e)
        {
            _showOnlyMyItems = false;
            UpdateFilterButtons();
            LoadRecipes();
        }

        /// <summary>
        /// Обработка клика по кнопке "Мои"
        /// </summary>
        private void MyButton_Click(object sender, RoutedEventArgs e)
        {
            _showOnlyMyItems = true;
            UpdateFilterButtons();
            LoadRecipes();
        }

        /// <summary>
        /// Обновление состояния кнопок фильтрации
        /// </summary>
        private void UpdateFilterButtons()
        {
            if (AllButton != null && MyButton != null)
            {
                if (_showOnlyMyItems)
                {
                    // Кнопка "Мои" активна
                    MyButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3498DB"));
                    MyButton.Foreground = new SolidColorBrush(Colors.White);
                    MyButton.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3498DB"));

                    // Кнопка "Все" неактивна
                    AllButton.Background = Brushes.Transparent;
                    AllButton.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2C3E50"));
                    AllButton.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E9ECEF"));
                }
                else
                {
                    // Кнопка "Все" активна
                    AllButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3498DB"));
                    AllButton.Foreground = new SolidColorBrush(Colors.White);
                    AllButton.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3498DB"));

                    // Кнопка "Мои" неактивна
                    MyButton.Background = Brushes.Transparent;
                    MyButton.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2C3E50"));
                    MyButton.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E9ECEF"));
                }
            }
        }

        /// <summary>
        /// Обработка поиска
        /// </summary>
        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_recipeItems == null)
                return;

            string searchText = SearchTextBox.Text?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(searchText))
            {
                _filteredItems = new List<RecipeWithSelection>(_recipeItems);
            }
            else
            {
                _filteredItems = _recipeItems
                    .FindAll(r => r.Recipe.Title.ToLower().Contains(searchText.ToLower()));
            }

            RecipesListBox.ItemsSource = _filteredItems;
            ErrorMessageHide();
        }

        /// <summary>
        /// Обработка клика по элементу рецепта
        /// </summary>
        private void RecipeItem_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is RecipeWithSelection item)
            {
                item.IsSelected = !item.IsSelected;
                RecipesListBox.Items.Refresh();
                ErrorMessageHide();
            }
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

        private void AddSelectedButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorMessageHide();

            var selectedRecipes = _filteredItems
                .Where(r => r.IsSelected)
                .Select(r => r.Recipe)
                .ToList();

            if (selectedRecipes.Count == 0)
            {
                ErrorMessageVisible("*Пожалуйста, выберите хотя бы один рецепт");
                return;
            }

            _callback?.OnRecipesSelected(selectedRecipes);

            if (NavigationService.CanGoBack)
                NavigationService.GoBack();
        }

        /// <summary>
        /// Обработка клика по кнопке редактирования
        /// </summary>
        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is RecipeWithSelection item)
            {
                if (!item.IsUserRecipe)
                {
                    MessageBox.Show("Вы не можете редактировать системный рецепт.", "Доступ запрещён",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                NavigationService.Navigate(new CreateRecipePage(item.Recipe));
            }
        }

        /// <summary>
        /// Обработка клика по кнопке удаления
        /// </summary>
        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is RecipeWithSelection item)
            {
                if (!item.IsUserRecipe)
                {
                    MessageBox.Show("Вы не можете удалить системный рецепт.", "Доступ запрещён",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var result = MessageBox.Show(
                    $"Вы уверены, что хотите удалить рецепт '{item.Recipe.Title}'?\n\n" +
                    $"Это действие нельзя отменить.",
                    "Подтверждение удаления",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    DeleteRecipe(item.Recipe);
                }
            }
        }

        /// <summary>
        /// Удаление рецепта (с каскадным удалением)
        /// </summary>
        private void DeleteRecipe(Recipe recipe)
        {
            try
            {
                using (var ctx = new TrackSyncContext())
                {
                    var recipeToRemove = ctx.Recipes.FirstOrDefault(r => r.RecipeId == recipe.RecipeId);

                    if (recipeToRemove != null)
                    {
                        // Проверка принадлежности
                        if (recipeToRemove.CreatorUserId != SessionManager.CurrentUserId)
                        {
                            MessageBox.Show("Вы не можете удалить этот рецепт.", "Доступ запрещён",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        // Просто удаляем - каскадное удаление произойдет автоматически
                        ctx.Recipes.Remove(recipeToRemove);
                        ctx.SaveChanges();

                        MessageBox.Show($"Рецепт '{recipe.Title}' успешно удалён.", "Готово",
                            MessageBoxButton.OK, MessageBoxImage.Information);

                        LoadRecipes();
                    }
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении рецепта:\n{ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

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