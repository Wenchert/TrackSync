using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using TrackSync.DataBase;
using TrackSync.DataBase.WorkWithDB;
using TrackSync.Windows;

namespace TrackSync.Pages
{
    public partial class NotificationsPanel : UserControl
    {
        // Вспомогательный класс для отображения приглашения
        public class InvitationDisplayItem
        {
            public int InvitationId { get; set; }
            public string Title { get; set; }
            public string DateRange { get; set; }
            public string Description { get; set; }

            // Свойство для управления видимостью описания
            public Visibility HasDescription =>
                !string.IsNullOrWhiteSpace(Description) ? Visibility.Visible : Visibility.Collapsed;
        }

        public event EventHandler InvitationsChanged;

        public NotificationsPanel()
        {
            InitializeComponent();
            LoadInvitations();
        }

        /// <summary>
        /// Загрузка приглашений пользователя
        /// </summary>
        private void LoadInvitations()
        {
            try
            {
                var invitations = TrackSyncManagerList.GetUserInvitations(SessionManager.CurrentUserId);

                if (invitations == null || invitations.Count == 0)
                {
                    // Показываем пустое состояние
                    EmptyState.Visibility = Visibility.Visible;
                    InvitationsList.Visibility = Visibility.Collapsed;
                    CountBadge.Visibility = Visibility.Collapsed;
                    return;
                }

                // Формируем список для отображения
                var displayItems = new List<InvitationDisplayItem>();
                foreach (var invitation in invitations)
                {
                    displayItems.Add(new InvitationDisplayItem
                    {
                        InvitationId = invitation.InvitationId,
                        Title = invitation.HikeRoom.Title,
                        DateRange = $"{invitation.HikeRoom.StartDate:dd.MM.yyyy} - {invitation.HikeRoom.EndDate:dd.MM.yyyy}",
                        Description = invitation.HikeRoom.Description ?? string.Empty
                    });
                }

                // Обновляем интерфейс
                CountBadgeText.Text = invitations.Count.ToString();
                CountBadge.Visibility = Visibility.Visible;
                EmptyState.Visibility = Visibility.Collapsed;
                InvitationsList.Visibility = Visibility.Visible;
                InvitationsList.ItemsSource = displayItems;

                InvitationsChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки приглашений: {ex.Message}", "Ошибка");
            }
        }

        private void AcceptButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int invitationId)
            {
                var result = TrackSyncManagerList.AcceptInvitation(invitationId, SessionManager.CurrentUserId);
                if (result)
                {
                    LoadInvitations();
                }
            }
        }

        private void RejectButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int invitationId)
            {
                // Отклоняем приглашение без подтверждения
                var success = TrackSyncManagerList.RejectInvitation(invitationId, SessionManager.CurrentUserId);
                if (success)
                {
                    LoadInvitations();
                }
            }
        }

        /// <summary>
        /// Обновить список приглашений
        /// </summary>
        public void Refresh()
        {
            LoadInvitations();
        }
    }
}