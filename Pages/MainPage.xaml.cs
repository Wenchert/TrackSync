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
        }

        //private void LoadUserRooms()
        //{
        //    try
        //    {
        //        var rooms = TrackSyncManagerList.GetUserRooms(SessionManager.CurrentUserId);

        //        if (rooms != null)
        //        {
        //            RoomsListBox.ItemsSource = rooms;
        //        }
        //        else
        //        {
        //            RoomsListBox.ItemsSource = new List<HikeRoom>();
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show($"Ошибка загрузки походов: {ex.Message}", "Ошибка");
        //    }
        //}

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

        /// <summary>
        /// Обработчик выбора комнаты
        /// </summary>
        private void UpcomingHikesControl_RoomSelected(object sender, HikeRoomEventArgs e)
        {
            // Переход на страницу деталей похода
            NavigationService?.Navigate(new HikeRoomPage(e.RoomId));
        }
    }
}