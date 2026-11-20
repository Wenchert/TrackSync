using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using TrackSync.DataBase.Model;
using TrackSync.DataBase.WorkWithDB;
using TrackSync.Functions;

namespace TrackSync.Pages
{
    public partial class InviteUsersPage : Page
    {
        public class UserWithSelection
        {
            public User User { get; set; }
            public bool IsSelected { get; set; }
        }

        private List<UserWithSelection> _allUsers;          
        private List<UserWithSelection> _filteredItems; 
        private readonly int _currentRoomId;

        public InviteUsersPage(int currentRoomId)
        {
            InitializeComponent();
            _currentRoomId = currentRoomId;
            LoadUsers();
        }

        private void LoadUsers()
        {
            var usersList = TrackSyncManagerList.GetUsersForInvitation(_currentRoomId, SessionManager.CurrentUserId);

            if (usersList != null && usersList.Count > 0)
            {
                _allUsers = usersList.ConvertAll(u => new UserWithSelection { User = u, IsSelected = false });
                _filteredItems = _allUsers.ToList(); 
                UsersListBox.ItemsSource = _filteredItems;
            }
            else
            {
                _allUsers = new List<UserWithSelection>();
                _filteredItems = new List<UserWithSelection>();
                UsersListBox.ItemsSource = _filteredItems;
                if (usersList != null)
                    ErrorMessageVisible("Нет доступных пользователей для приглашения.");
            }
            UpdateSelectedCount();
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = SearchTextBox.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(searchText))
            {
                _filteredItems = _allUsers.ToList();
            }
            else
            {
                var lower = searchText.ToLower();
                _filteredItems = _allUsers
                    .Where(u => u.User.Name.ToLower().Contains(lower) || u.User.Email.ToLower().Contains(lower))
                    .ToList();
            }
            UsersListBox.ItemsSource = _filteredItems;
            ErrorMessageHide();
        }

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

        private void UserCheckBox_Click(object sender, RoutedEventArgs e)
        {
            UpdateSelectedCount();
            ErrorMessageHide();
        }

        private void UpdateSelectedCount()
        {
            var selectedCount = _allUsers?.Count(u => u.IsSelected) ?? 0;
            SelectedCountText.Text = $"Выбрано: {selectedCount} {(selectedCount == 1 ? "пользователь" : selectedCount < 5 ? "пользователя" : "пользователей")}";
        }

        private void InviteUsersButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedUsers = _allUsers?.Where(u => u.IsSelected).Select(u => u.User).ToList();

            if (selectedUsers == null || selectedUsers.Count == 0)
            {
                ErrorMessageVisible("*Пожалуйста, выберите хотя бы одного пользователя для приглашения");
                return;
            }

            var userIds = selectedUsers.Select(u => u.UserId).ToList();

            int invitationsCreated, invitationsSkipped;
            bool success = TrackSyncManagerList.SendInvitations(userIds, _currentRoomId, out invitationsCreated, out invitationsSkipped);

            if (success)
            {
                if (NavigationService.CanGoBack)
                    NavigationService.GoBack();
            }
            else
            {
                ErrorMessageVisible("Не удалось отправить приглашения. Проверьте подключение к базе данных.");
            }
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