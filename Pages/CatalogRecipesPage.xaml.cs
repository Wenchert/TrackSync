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
        private List<Participant> _roomParticipants;
        private readonly IRecipeCallback _callback;
        private readonly int _roomId;
        private bool _showOnlyMyItems = false;
        private RecipeWithSelection _selectedRecipe;
        private List<int> _selectedParticipantIds = new List<int>();

        public CatalogRecipesPage(IRecipeCallback callback, int roomId)
        {
            InitializeComponent();
            _callback = callback;
            _roomId = roomId;

            // Скрываем кнопку "Добавить рецепт" пока не выбран рецепт
            if (AddSelectedButton != null)
                AddSelectedButton.Visibility = System.Windows.Visibility.Visible;

            LoadRecipes();
            LoadParticipants();
            UpdateFilterButtons();
        }

        public CatalogRecipesPage()
        {
            InitializeComponent();
            _callback = null;
            _roomId = 0;

            // В обычном режиме скрываем кнопку добавления
            if (AddSelectedButton != null)
                AddSelectedButton.Visibility = System.Windows.Visibility.Collapsed;

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

        private void LoadParticipants()
        {
            if (_roomId == 0) return;

            // Используем существующий метод для загрузки участников
            _roomParticipants = TrackSyncManagerList.GetParticipantsByRoomId(_roomId)
                ?? new List<Participant>();
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

        private void RecipeItem_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is RecipeWithSelection item)
            {
                // Снимаем выбор со всех рецептов
                foreach (var recipe in _filteredItems)
                    recipe.IsSelected = false;

                // Выбираем только текущий
                item.IsSelected = true;
                _selectedRecipe = item;
                RecipesListBox.Items.Refresh();
                ErrorMessageHide();

                // Если есть комната, показываем панель выбора ответственных
                if (_roomId > 0)
                {
                    ShowResponsibleSelectionPanel();
                }
            }
        }

        private void ShowResponsibleSelectionPanel()
        {
            if (_roomParticipants == null || !_roomParticipants.Any())
            {
                ErrorMessageVisible("В комнате нет участников для выбора");
                return;
            }

            // Сбрасываем предыдущие выборы
            _selectedParticipantIds.Clear();

            // Заполняем CheckBox'ы для каждого участника
            ParticipantsList.Children.Clear();
            foreach (var participant in _roomParticipants)
            {
                if (participant?.User == null) continue;

                var checkBox = new CheckBox
                {
                    Content = participant.User.Name ?? "Неизвестный пользователь",
                    Tag = participant.ParticipantId,
                    Margin = new Thickness(5, 2, 5, 2),
                    FontSize = 14
                };
                checkBox.Checked += ParticipantCheckBox_Checked;
                checkBox.Unchecked += ParticipantCheckBox_Unchecked;

                ParticipantsList.Children.Add(checkBox);
            }

            // Показываем панель выбора
            ResponsibleSelectionPanel.Visibility = System.Windows.Visibility.Visible;

            // Прокручиваем к панели выбора
            Dispatcher.BeginInvoke(new System.Action(() =>
            {
                ResponsibleSelectionPanel.BringIntoView();
            }), System.Windows.Threading.DispatcherPriority.Background);
        }

        private void ParticipantCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.Tag is int participantId)
            {
                if (!_selectedParticipantIds.Contains(participantId))
                {
                    _selectedParticipantIds.Add(participantId);
                }
            }
        }

        private void ParticipantCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.Tag is int participantId)
            {
                _selectedParticipantIds.Remove(participantId);
            }
        }

        private void HideResponsibleSelectionPanel()
        {
            ResponsibleSelectionPanel.Visibility = System.Windows.Visibility.Collapsed;
            _selectedRecipe = null;

            // Снимаем выделение с рецептов
            foreach (var recipe in _filteredItems)
                recipe.IsSelected = false;
            RecipesListBox.Items.Refresh();
        }

        private void AddSelectedButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedRecipe == null)
            {
                ErrorMessageVisible("Пожалуйста, выберите рецепт");
                return;
            }

            // Проверяем, выбраны ли ответственные
            if (_selectedParticipantIds == null || !_selectedParticipantIds.Any())
            {
                ErrorMessageVisible("Выберите хотя бы одного ответственного участника");
                return;
            }

            // Передаём выбранный рецепт и ответственных обратно в раскладку
            _callback?.OnRecipeSelected(_selectedRecipe.Recipe, _selectedParticipantIds);

            if (NavigationService?.CanGoBack == true)
                NavigationService.GoBack();
        }

        private void ConfirmResponsibleButton_Click(object sender, RoutedEventArgs e)
        {
            AddSelectedButton_Click(sender, e);
        }

        private void CancelResponsibleButton_Click(object sender, RoutedEventArgs e)
        {
            HideResponsibleSelectionPanel();
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
}