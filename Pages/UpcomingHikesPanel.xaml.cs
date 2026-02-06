using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using TrackSync.DataBase.WorkWithDB;
using TrackSync.Windows;

namespace TrackSync.Pages
{
    public partial class UpcomingHikesPanel : UserControl
    {
        public event EventHandler<HikeRoomEventArgs> RoomSelected; // Событие выбора комнаты

        public UpcomingHikesPanel()
        {
            InitializeComponent();
            LoadRooms();
        }

        /// <summary>
        /// Загрузка списка комнат пользователя
        /// </summary>
        private void LoadRooms()
        {
            try
            {
                var rooms = TrackSyncManagerList.GetUserRooms(SessionManager.CurrentUserId);

                if (rooms != null)
                {
                    RoomsListBox.ItemsSource = rooms;
                }
                else
                {
                    RoomsListBox.ItemsSource = new List<HikeRoom>();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки походов: {ex.Message}", "Ошибка");
            }
        }

        /// <summary>
        /// Обработчик клика по элементу комнаты
        /// </summary>
        private void RoomItem_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is int roomId)
            {
                RoomSelected?.Invoke(this, new HikeRoomEventArgs(roomId));
            }
        }
    }

        /// <summary>
        /// Аргументы события выбора комнаты
        /// </summary>
        public class HikeRoomEventArgs : EventArgs
        {
            public int RoomId { get; set; }

            public HikeRoomEventArgs(int roomId)
            {
                RoomId = roomId;
            }
        }
}