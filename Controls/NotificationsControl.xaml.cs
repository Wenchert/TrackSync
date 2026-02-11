using System.Windows;
using System.Windows.Controls;
using TrackSync.DataBase.WorkWithDB;
using TrackSync.DataBase.Model;
using TrackSync.Functions;

namespace TrackSync.Controls
{
    public partial class NotificationsPanel : UserControl
    {
        // Тип уведомления
        public enum NotificationType { Invitation, Bid }

        // Базовый класс для отображения уведомлений
        public class NotificationDisplayItem
        {
            public int Id { get; set; }
            public NotificationType NotificationType { get; set; }
            public string Title { get; set; }
            public string DateRange { get; set; }
            public string Description { get; set; }
            public string Email { get; set; } // Только для заявок

            // Вспомогательные свойства для управления видимостью
            public Visibility DescriptionVisibility =>
                NotificationType == NotificationType.Invitation && !string.IsNullOrWhiteSpace(Description)
                    ? Visibility.Visible : Visibility.Collapsed;

            public Visibility EmailVisibility =>
                NotificationType == NotificationType.Bid && !string.IsNullOrWhiteSpace(Email)
                    ? Visibility.Visible : Visibility.Collapsed;
        }

        public event EventHandler InvitationsChanged;

        public NotificationsPanel()
        {
            InitializeComponent();
            LoadNotifications();
        }

        /// <summary>
        /// Загрузка всех уведомлений: приглашений и заявок
        /// </summary>
        public void LoadNotifications()
        {
            try
            {
                var allNotifications = new List<NotificationDisplayItem>();

                // 1. Загружаем приглашения пользователя
                var invitations = TrackSyncManagerList.GetUserInvitations(SessionManager.CurrentUserId);
                if (invitations != null)
                {
                    foreach (var invitation in invitations)
                    {
                        allNotifications.Add(new NotificationDisplayItem
                        {
                            Id = invitation.InvitationId,
                            NotificationType = NotificationType.Invitation,
                            Title = invitation.HikeRoom.Title,
                            DateRange = $"{invitation.HikeRoom.StartDate:dd.MM.yyyy} - {invitation.HikeRoom.EndDate:dd.MM.yyyy}",
                            Description = invitation.HikeRoom.Description ?? string.Empty
                        });
                    }
                }

                // 2. Загружаем заявки (только если пользователь является лидером хотя бы одной комнаты)
                var bids = TrackSyncManagerList.GetUserBids(SessionManager.CurrentUserId);
                if (bids != null)
                {
                    foreach (var bid in bids)
                    {
                        allNotifications.Add(new NotificationDisplayItem
                        {
                            Id = bid.BidId,
                            NotificationType = NotificationType.Bid,
                            Title = bid.HikeRoom.Title,
                            DateRange = $"{bid.HikeRoom.StartDate:dd.MM.yyyy} - {bid.HikeRoom.EndDate:dd.MM.yyyy}",
                            Email = bid.User.Name // Отображаем email заявителя
                        });
                    }
                }

                // Обновляем интерфейс
                if (allNotifications.Count == 0)
                {
                    EmptyState.Visibility = Visibility.Visible;
                    NotificationsList.Visibility = Visibility.Collapsed;
                }
                else
                {
                    EmptyState.Visibility = Visibility.Collapsed;
                    NotificationsList.ItemsSource = allNotifications;
                    NotificationsList.Visibility = Visibility.Visible;
                }

                InvitationsChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки уведомлений: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AcceptButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int id)
            {
                // Определяем тип уведомления по контексту данных кнопки
                var container = ItemsControl.ContainerFromElement(NotificationsList, button) as FrameworkElement;
                if (container?.DataContext is NotificationDisplayItem item)
                {
                    bool success = false;
                    if (item.NotificationType == NotificationType.Invitation)
                    {
                        success = TrackSyncManagerList.AcceptInvitation(id, SessionManager.CurrentUserId);
                    }
                    else // Bid
                    {
                        success = TrackSyncManagerList.AcceptBid(id, SessionManager.CurrentUserId);
                    }

                    if (success)
                    {
                        LoadNotifications();
                    }
                }
            }
        }

        private void RejectButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int id)
            {
                var container = ItemsControl.ContainerFromElement(NotificationsList, button) as FrameworkElement;
                if (container?.DataContext is NotificationDisplayItem item)
                {
                    bool success = false;
                    if (item.NotificationType == NotificationType.Invitation)
                    {
                        success = TrackSyncManagerList.RejectInvitation(id, SessionManager.CurrentUserId);
                    }
                    else // Bid
                    {
                        success = TrackSyncManagerList.RejectBid(id);
                    }

                    if (success)
                    {
                        LoadNotifications();
                    }
                }
            }
        }

        // Проверка, что пользователь уже есть в комнате
        public static bool IsUserAlreadyParticipant(int userId, int roomId)
        {
            try
            {
                using (var ctx = new TrackSyncContext())
                {
                    return ctx.Participants
                        .Any(p => p.UserId == userId && p.HikeRoomId == roomId);
                }
            }
            catch
            {
                return false;
            }
        }

        // Добавление участника в комнату (права отсутствуют)
        public static bool AddParticipantSafely(int userId, int roomId, bool
            editMealPlan = false, bool editTrack = false, bool editEquipment = false)
        {
            try
            {
                using (var ctx = new TrackSyncContext())
                {
                    // Проверяем, не является ли пользователь уже участником
                    if (ctx.Participants.Any(p => p.UserId == userId && p.HikeRoomId == roomId))
                    {
                        return true; // Уже участник, считаем успехом
                    }

                    // Создаем нового участника
                    var participant = new Participant(
                        userId: userId,
                        hikeRoomId: roomId,
                        editMealPlan: editMealPlan,
                        editTrack: editTrack,
                        editEquipment: editEquipment
                    );

                    ctx.Participants.Add(participant);
                    ctx.SaveChanges();
                    return true;
                }
            }
            catch (Exception ex)
            {
                // Проверяем, не является ли ошибка нарушением уникальности
                if (ex.InnerException?.Message?.Contains("duplicate key") == true ||
                    ex.Message.Contains("duplicate key"))
                {
                    return true;
                }

                MessageBox.Show($"Ошибка добавления участника: {ex.Message}", "Ошибка");
                return false;
            }
        }
    }
}