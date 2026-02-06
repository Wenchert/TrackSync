using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using TrackSync.DataBase;
using TrackSync.DataBase.WorkWithDB;
using TrackSync.Interfaces;
using TrackSync.Windows;

namespace TrackSync.Pages
{
    public partial class CatalogRecipesPage : Page
    {
        public class RecipeWithSelection
        {
            public Recipe Recipe { get; set; }
            public bool IsSelected { get; set; }
            public bool IsUserRecipe => Recipe.CreatorUserId == SessionManager.CurrentUserId;
            public System.Windows.Visibility ActionButtonsVisibility =>
                IsUserRecipe ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        }

        private List<RecipeWithSelection> _recipeItems;
        private List<RecipeWithSelection> _filteredItems;
        private readonly IRecipeCallback _callback;
        private readonly int _roomId;
        private bool _showOnlyMyItems = false;
        private RecipeWithSelection _selectedRecipe;

        // Конструктор для выбора рецепта в раскладке
        public CatalogRecipesPage(IRecipeCallback callback, int roomId)
        {
            InitializeComponent();
            _callback = callback;
            _roomId = roomId;

            // Делаем кнопку "Добавить выбранное" видимой
            if (AddSelectedButton != null)
                AddSelectedButton.Visibility = System.Windows.Visibility.Visible;

            LoadRecipes();
            UpdateFilterButtons();
        }

        // Оригинальный конструктор для других сценариев
        public CatalogRecipesPage()
        {
            InitializeComponent();
            _callback = null;
            _roomId = 0;
            LoadRecipes();
            UpdateFilterButtons();
        }

        private void LoadRecipes()
        {
            List<Recipe> recipeList = _showOnlyMyItems
                ? TrackSyncManagerList.GetUserRecipes(SessionManager.CurrentUserId)
                : TrackSyncManagerList.GetAllRecipesForUser(SessionManager.CurrentUserId);

            _recipeItems = recipeList?.ConvertAll(r => new RecipeWithSelection { Recipe = r, IsSelected = false })
                ?? new List<RecipeWithSelection>();

            _filteredItems = new List<RecipeWithSelection>(_recipeItems);
            RecipesListBox.ItemsSource = _filteredItems;
        }

        private void AllButton_Click(object sender, RoutedEventArgs e)
        {
            _showOnlyMyItems = false;
            UpdateFilterButtons();
            LoadRecipes();
        }

        private void MyButton_Click(object sender, RoutedEventArgs e)
        {
            _showOnlyMyItems = true;
            UpdateFilterButtons();
            LoadRecipes();
        }

        private void UpdateFilterButtons()
        {
            if (AllButton == null || MyButton == null) return;

            if (_showOnlyMyItems)
            {
                MyButton.Background = System.Windows.Media.Brushes.DodgerBlue;
                MyButton.Foreground = System.Windows.Media.Brushes.White;
                MyButton.BorderBrush = System.Windows.Media.Brushes.DodgerBlue;
                AllButton.Background = System.Windows.Media.Brushes.Transparent;
                AllButton.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(44, 62, 80));
                AllButton.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(233, 236, 239));
            }
            else
            {
                AllButton.Background = System.Windows.Media.Brushes.DodgerBlue;
                AllButton.Foreground = System.Windows.Media.Brushes.White;
                AllButton.BorderBrush = System.Windows.Media.Brushes.DodgerBlue;
                MyButton.Background = System.Windows.Media.Brushes.Transparent;
                MyButton.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(44, 62, 80));
                MyButton.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(233, 236, 239));
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_recipeItems == null) return;

            string searchText = SearchTextBox.Text?.Trim() ?? "";
            _filteredItems = string.IsNullOrWhiteSpace(searchText)
                ? new List<RecipeWithSelection>(_recipeItems)
                : _recipeItems.FindAll(r => r.Recipe.Title.ToLower().Contains(searchText.ToLower()));

            RecipesListBox.ItemsSource = _filteredItems;
            ErrorMessageHide();
        }

        // Одиночный выбор рецепта (снимаем выбор со всех кроме текущего)
        private void RecipeItem_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is RecipeWithSelection item)
            {
                // Снимаем выбор со всех
                foreach (var recipe in _filteredItems)
                    recipe.IsSelected = false;

                // Выбираем только текущий
                item.IsSelected = true;
                _selectedRecipe = item;
                RecipesListBox.Items.Refresh();
                ErrorMessageHide();
            }
        }

        // Основная логика: выбор одного рецепта + выбор ответственных через диалог
        private void AddSelectedButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedRecipe == null)
            {
                ErrorMessageVisible("Пожалуйста, выберите рецепт");
                return;
            }

            // Показываем диалог выбора ответственных
            var dialog = new SelectResponsibleDialog(_roomId);
            if (dialog.ShowDialog() == true && dialog.SelectedParticipantIds.Any())
            {
                // Передаём выбранный рецепт и ответственных обратно в раскладку
                _callback?.OnRecipeSelected(_selectedRecipe.Recipe, dialog.SelectedParticipantIds);

                if (NavigationService?.CanGoBack == true)
                    NavigationService.GoBack();
            }
            else if (!dialog.SelectedParticipantIds.Any())
            {
                ErrorMessageVisible("Выберите хотя бы одного ответственного участника");
            }
        }

        private void CreateRecipeButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new CreateRecipePage());
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService?.CanGoBack == true)
                NavigationService.GoBack();
        }

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
                NavigationService?.Navigate(new CreateRecipePage(item.Recipe));
            }
        }

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
                    $"Вы уверены, что хотите удалить рецепт '{item.Recipe.Title}'?\nЭто действие нельзя отменить.",
                    "Подтверждение удаления",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        using var ctx = new TrackSyncContext();
                        var recipeToRemove = ctx.Recipes.FirstOrDefault(r => r.RecipeId == item.Recipe.RecipeId);

                        if (recipeToRemove?.CreatorUserId == SessionManager.CurrentUserId)
                        {
                            ctx.Recipes.Remove(recipeToRemove);
                            ctx.SaveChanges();
                            MessageBox.Show($"Рецепт '{item.Recipe.Title}' успешно удалён.", "Готово",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                            LoadRecipes();
                        }
                        else
                        {
                            MessageBox.Show("Вы не можете удалить этот рецепт.", "Доступ запрещён",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                    catch (System.Exception ex)
                    {
                        MessageBox.Show($"Ошибка при удалении рецепта:\n{ex.Message}", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void ErrorMessageVisible(string message)
        {
            if (ErrorMessage != null)
            {
                ErrorMessage.Visibility = System.Windows.Visibility.Visible;
                ErrorMessage.Text = message;
            }
        }

        private void ErrorMessageHide()
        {
            if (ErrorMessage != null)
                ErrorMessage.Visibility = System.Windows.Visibility.Collapsed;
        }
    }

    // Простой диалог для выбора ответственных участников
    public class SelectResponsibleDialog : Window
    {
        public List<int> SelectedParticipantIds { get; private set; } = new();

        public SelectResponsibleDialog(int roomId)
        {
            Title = "Выбор ответственных участников";
            Width = 400;
            Height = 350;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;

            var stackPanel = new StackPanel { Margin = new Thickness(20) };

            var title = new TextBlock
            {
                Text = "Выберите ответственных за рецепт:",
                FontWeight = System.Windows.FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 15),
                FontSize = 14
            };
            stackPanel.Children.Add(title);

            // Загружаем участников комнаты
            List<Participant> participants;
            using (var ctx = new TrackSyncContext())
            {
                participants = ctx.Participants
                    .Where(p => p.HikeRoomId == roomId)
                    .Include(p => p.User)
                    .Distinct() // Убедимся, что нет дубликатов
                    .ToList();
            }

            // Проверяем, что участники действительно загружены
            if (!participants.Any())
            {
                stackPanel.Children.Add(new TextBlock
                {
                    Text = "В комнате нет участников",
                    Foreground = System.Windows.Media.Brushes.Red,
                    Margin = new Thickness(0, 10, 0, 10)
                });
            }
            else
            {
                // Создаём чекбоксы для каждого участника
                foreach (var participant in participants)
                {
                    var checkBox = new CheckBox
                    {
                        Content = $"{participant.User.Name} (ID: {participant.ParticipantId})",
                        Tag = participant.ParticipantId,
                        Margin = new Thickness(0, 5, 0, 5),
                        FontSize = 13
                    };
                    stackPanel.Children.Add(checkBox);
                }
            }

            // Кнопки действий
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 20, 0, 0)
            };

            var cancelButton = new Button
            {
                Content = "Отмена",
                Width = 80,
                Margin = new Thickness(0, 0, 10, 0),
                IsCancel = true
            };
            cancelButton.Click += (s, e) => DialogResult = false;

            var okButton = new Button
            {
                Content = "Выбрать",
                Width = 80,
                IsDefault = true,
                Background = System.Windows.Media.Brushes.DodgerBlue,
                Foreground = System.Windows.Media.Brushes.White
            };
            okButton.Click += (s, e) =>
            {
                // Собираем выбранные ID участников
                SelectedParticipantIds.Clear();

                foreach (var child in stackPanel.Children)
                {
                    if (child is CheckBox checkBox && checkBox.IsChecked == true)
                    {
                        if (checkBox.Tag is int participantId)
                        {
                            SelectedParticipantIds.Add(participantId);
                        }
                    }
                }

                if (SelectedParticipantIds.Any())
                {
                    DialogResult = true;
                }
                else
                {
                    MessageBox.Show("Выберите хотя бы одного ответственного участника",
                                  "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            };

            buttonPanel.Children.Add(cancelButton);
            buttonPanel.Children.Add(okButton);
            stackPanel.Children.Add(buttonPanel);

            Content = stackPanel;
        }
    }
}