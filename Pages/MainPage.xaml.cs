using System.Windows;
using System.Windows.Controls;
using TrackSync.DataBase.WorkWithDB;
using TrackSync.Windows;
using static TrackSync.DataBase.WorkWithDB.TrackSyncManagerList;

namespace TrackSync.Pages
{
    public partial class MainPage : Page
    {
        public MainPage()
        {
            InitializeComponent();
            LoadUserRooms();
        }

        private void LoadUserRooms()
        {
            var rooms = TrackSyncManagerList.GetUserRooms(SessionManager.CurrentUserId);
            RoomsListBox.ItemsSource = rooms;
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

        private void ShowAllNotifications_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Все уведомления", "TrackSync", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ShowAllHikes_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Все походы", "TrackSync", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}