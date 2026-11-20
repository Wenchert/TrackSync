using System.Windows;
using System.Windows.Controls;
using TrackSync.DataBase.WorkWithDB;
using TrackSync.DataBase.Model;
using TrackSync.Functions;

namespace TrackSync.Controls
{
    public partial class NotificationsPanel : UserControl
    {
        public enum NotificationType { Invitation, Bid }

        public class NotificationDisplayItem
        {
            public int Id { get; set; }
            public NotificationType NotificationType { get; set; }
            public string Title { get; set; }
            public string DateRange { get; set; }
            public string Description { get; set; }
            public string Email { get; set; }

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

        public void LoadNotifications()
        {
            try
            {
                var allNotifications = new List<NotificationDisplayItem>();

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
                            Email = bid.User.Name 
                        });
                    }
                }

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
                var container = ItemsControl.ContainerFromElement(NotificationsList, button) as FrameworkElement;
                if (container?.DataContext is NotificationDisplayItem item)
                {
                    bool success = false;
                    if (item.NotificationType == NotificationType.Invitation)
                    {
                        success = TrackSyncManagerList.AcceptInvitation(id, SessionManager.CurrentUserId);
                    }
                    else
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
                    else
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

        public static bool AddParticipantSafely(int userId, int roomId, bool
            editMealPlan = false, bool editTrack = false, bool editEquipment = false)
        {
            try
            {
                using (var ctx = new TrackSyncContext())
                {
                    if (ctx.Participants.Any(p => p.UserId == userId && p.HikeRoomId == roomId))
                    {
                        return true;
                    }

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