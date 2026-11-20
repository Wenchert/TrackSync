using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TrackSync.DataBase;
using TrackSync.DataBase.Model;
using TrackSync.DataBase.WorkWithDB;
using TrackSync.Functions;
using TrackSync.Interface;

namespace TrackSync.Pages
{
    public partial class CatalogIngredientsPage : Page
    {
        // Вспомогательный класс для отслеживания выбора
        public class IngredientWithSelection
        {
            public Ingredient Ingredient { get; set; }
            public bool IsSelected { get; set; }

            public bool IsUserIngredient
            {
                get
                {
                    return Ingredient.CreatorUserId == SessionManager.CurrentUserId;
                }
            }

            public System.Windows.Visibility ActionButtonsVisibility
            {
                get
                {
                    return IsUserIngredient ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
                }
            }
        }

        private List<IngredientWithSelection> _ingredientItems;
        private List<IngredientWithSelection> _filteredItems;
        private readonly IRecipeIngredientCallback _callback;
        private bool _showOnlyMyItems = false;

        public CatalogIngredientsPage(IRecipeIngredientCallback callback)
        {
            InitializeComponent();
            _callback = callback;

            this.Loaded += (s, e) =>
            {
                LoadIngredients();
                UpdateFilterButtons();
            };

            LoadIngredients();
            UpdateFilterButtons();
        }

        private void CatalogIngredientsPage_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (this.IsVisible)
            {
                LoadIngredients();
                UpdateFilterButtons();
            }
        }

        private void LoadIngredients()
        {
            List<Ingredient> ingredientList;

            if (_showOnlyMyItems)
            {
                // Показываем только ингредиенты, созданные текущим пользователем
                ingredientList = TrackSyncManagerList.GetUserIngredients(SessionManager.CurrentUserId);
            }
            else
            {
                // Показываем все ингредиенты (системные + созданные пользователем)
                ingredientList = TrackSyncManagerList.GetAllIngredientsForUser(SessionManager.CurrentUserId);
            }

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
            else
            {
                IngredientsListBox.ItemsSource = new List<IngredientWithSelection>();
            }
        }

        private void AllButton_Click(object sender, RoutedEventArgs e)
        {
            _showOnlyMyItems = false;
            UpdateFilterButtons();
            LoadIngredients();
        }

        private void MyButton_Click(object sender, RoutedEventArgs e)
        {
            _showOnlyMyItems = true;
            UpdateFilterButtons();
            LoadIngredients();
        }

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
            ErrorMessageHide();
        }

        private void IngredientItem_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is IngredientWithSelection item)
            {
                item.IsSelected = !item.IsSelected;
                IngredientsListBox.Items.Refresh();
                ErrorMessageHide();
            }
        }

        private void CreateIngredientButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new CreateIngredientPage());
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService.CanGoBack)
                NavigationService.GoBack();
        }

        private void AddSelectedButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorMessageHide();

            var selectedIngredients = _filteredItems
                .Where(i => i.IsSelected)
                .Select(i => i.Ingredient)
                .ToList();

            if (selectedIngredients.Count == 0)
            {
                ErrorMessageVisible("*Пожалуйста, выберите хотя бы один ингредиент");
                return;
            }

            _callback?.OnIngredientsSelected(selectedIngredients);

            if (NavigationService.CanGoBack)
                NavigationService.GoBack();
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is IngredientWithSelection item)
            {
                NavigationService.Navigate(new CreateIngredientPage(item.Ingredient));
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is IngredientWithSelection item)
            {
                DeleteIngredient(item.Ingredient);
            }
        }

        private void DeleteIngredient(Ingredient ingredient)
        {
            try
            {
                using (var ctx = new TrackSyncContext())
                {
                    var ingredientToRemove = ctx.Ingredients.FirstOrDefault(i => i.IngredientId == ingredient.IngredientId);

                    if (ingredientToRemove != null)
                    {
                        ctx.Ingredients.Remove(ingredientToRemove);
                        ctx.SaveChanges();
                        LoadIngredients();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении:\n{ex.Message}", "Ошибка",
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