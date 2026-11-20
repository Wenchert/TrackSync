using System.Windows;
using System.Windows.Controls;
using TrackSync.Controls;
using TrackSync.DataBase.WorkWithDB;
using TrackSync.Functions;
using TrackSync.Windows;

namespace TrackSync.Pages
{
    public partial class MainPage : Page
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LoadUserName();

            if (NotificationsControl != null)
            {
                NotificationsControl.InvitationsChanged += NotificationsControl_InvitationsChanged;
            }
        }

        private void NotificationsControl_InvitationsChanged(object sender, EventArgs e)
        {
            if (UpcomingHikesControl != null)
            {
                UpcomingHikesControl.LoadRooms();
            }
        }

        private void LoadUserName()
        {
            try
            {
                var currentUser = SessionManager.CurrentUser;

                if (currentUser != null && !string.IsNullOrWhiteSpace(currentUser.Name))
                {
                    WelcomeTextBlock.Text = $"Добро пожаловать, {currentUser.Name}";
                }
            }
            catch (System.Exception ex)
            {
                WelcomeTextBlock.Text = "Добро пожаловать!";
            }
        }

        private void RoomItem_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            OpenRoomFromSender(sender);
        }

        private void OpenRoomButton_Click(object sender, RoutedEventArgs e)
        {
            OpenRoomFromSender(sender);
        }

        private void OpenRoomFromSender(object sender)
        {
            int roomId = 0;

            if (sender is Border border && border.Tag is int id1)
                roomId = id1;
            else if (sender is Button button && button.Tag is int id2)
                roomId = id2;

            if (roomId > 0)
                FrameContext.MainWindowFrame.Navigate(new HikeRoomPage(roomId));
        }

        private void UpcomingHikesControl_RoomSelected(object sender, HikeRoomEventArgs e)
        {
            NavigationService?.Navigate(new HikeRoomPage(e.RoomId));
        }

        private void NotificationsControl_Loaded(object sender, RoutedEventArgs e)
        {

        }
    }
}