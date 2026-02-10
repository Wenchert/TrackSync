using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using TrackSync.DataBase.WorkWithDB;
using TrackSync.Windows;

namespace TrackSync.Pages
{
    public partial class InviteUsersPage : Page
    {
        // Вспомогательный класс для отслеживания выбора пользователей
        public class UserWithSelection
        {
            public User User { get; set; }
            public bool IsSelected { get; set; }
        }

        private List<UserWithSelection> _userItems;
        private List<UserWithSelection> _filteredItems;
        private readonly int _currentRoomId;

        public InviteUsersPage(int currentRoomId)
        {
            InitializeComponent();
            _currentRoomId = currentRoomId;
            LoadUsers();
        }

        /// <summary>
        /// Загрузка пользователей из базы данных через менеджер
        /// </summary>
        private void LoadUsers()
        {
            var usersList = TrackSyncManagerList.GetUsersForInvitation(_currentRoomId, SessionManager.CurrentUserId);

            if (usersList != null && usersList.Count > 0)
            {
                _userItems = usersList.ConvertAll(u => new UserWithSelection
                {
                    User = u,
                    IsSelected = false
                });

                _filteredItems = new List<UserWithSelection>(_userItems);
                UsersListBox.ItemsSource = _filteredItems;
            }
            else
            {
                UsersListBox.ItemsSource = new List<UserWithSelection>();
                if (usersList != null) // usersList == null означает ошибку, которая уже показана
                {
                    ErrorMessageVisible("Нет доступных пользователей для приглашения.");
                }
            }

            UpdateSelectedCount();
        }

        /// <summary>
        /// Обработка поиска через менеджер
        /// </summary>
        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = SearchTextBox.Text?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(searchText))
            {
                // Если поиск пустой, загружаем всех пользователей
                _filteredItems = new List<UserWithSelection>(_userItems);
            }
            else
            {
                // Используем метод поиска из менеджера
                var searchResults = TrackSyncManagerList.SearchUsersForInvitation(
                    searchText, _currentRoomId, SessionManager.CurrentUserId);

                if (searchResults != null)
                {
                    _filteredItems = searchResults.ConvertAll(u => new UserWithSelection
                    {
                        User = u,
                        IsSelected = _userItems?.FirstOrDefault(item => item.User.UserId == u.UserId)?.IsSelected ?? false
                    });
                }
                else
                {
                    // Ошибка уже показана в менеджере, просто очищаем список
                    _filteredItems = new List<UserWithSelection>();
                }
            }

            UsersListBox.ItemsSource = _filteredItems;
            ErrorMessageHide();
        }

        /// <summary>
        /// Обработка клика по элементу пользователя
        /// </summary>
        private void UserItem_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is UserWithSelection item)
            {
                item.IsSelected = !item.IsSelected;
                UsersListBox.Items.Refresh();
                UpdateSelectedCount();
                ErrorMessageHide();
            }
        }

        /// <summary>
        /// Обработка изменения состояния CheckBox
        /// </summary>
        private void UserCheckBox_Click(object sender, RoutedEventArgs e)
        {
            UpdateSelectedCount();
            ErrorMessageHide();
        }

        /// <summary>
        /// Обновление счетчика выбранных пользователей
        /// </summary>
        private void UpdateSelectedCount()
        {
            var selectedCount = _userItems?.Count(u => u.IsSelected) ?? 0;
            SelectedCountText.Text = $"Выбрано: {selectedCount} {(selectedCount == 1 ? "пользователь" : selectedCount < 5 ? "пользователя" : "пользователей")}";
        }

        /// <summary>
        /// Отправка приглашений через менеджер
        /// </summary>
        private void InviteUsersButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedUsers = _userItems?.Where(u => u.IsSelected).Select(u => u.User).ToList();

            if (selectedUsers == null || selectedUsers.Count == 0)
            {
                ErrorMessageVisible("*Пожалуйста, выберите хотя бы одного пользователя для приглашения");
                return;
            }

            var userIds = selectedUsers.Select(u => u.UserId).ToList();

            if (TrackSyncManagerList.SendInvitations(userIds, _currentRoomId, out int created, out int skipped))
            {
                if (created > 0)
                {
                    string message = $"Успешно отправлено {created} приглашени{(created == 1 ? "е" : created < 5 ? "я" : "й")}!";
                    if (skipped > 0)
                    {
                        message += $"\n{skipped} приглашени{(skipped == 1 ? "е" : skipped < 5 ? "я" : "й")} пропущено.";
                    }
                }
                else if (skipped > 0)
                {
                    MessageBox.Show($"Все выбранные пользователи уже связаны с этой комнатой.",
                        "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                // Возвращаемся назад
                if (NavigationService.CanGoBack)
                    NavigationService.GoBack();
            }
            // Ошибка уже показана в менеджере
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService.CanGoBack)
                NavigationService.GoBack();
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