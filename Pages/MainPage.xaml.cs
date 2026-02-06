using System.Windows;
using System.Windows.Controls;
using TrackSync.DataBase.WorkWithDB;
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
            // Загружаем имя пользователя из сессии
            LoadUserName();
        }

        /// <summary>
        /// Загружает имя пользователя из сессии и обновляет приветствие
        /// </summary>
        private void LoadUserName()
        {
            try
            {
                // Получаем текущего пользователя из сессии
                var currentUser = SessionManager.CurrentUser;

                if (currentUser != null && !string.IsNullOrWhiteSpace(currentUser.Name))
                {
                    // Формируем приветствие с именем пользователя
                    WelcomeTextBlock.Text = $"Добро пожаловать, {currentUser.Name}";
                }
            }
            catch (System.Exception ex)
            {
                // В случае ошибки показываем стандартное приветствие
                WelcomeTextBlock.Text = "Добро пожаловать!";
            }
        }

        /// <summary>
        /// Обработчик клика по элементу комнаты
        /// </summary>
        private void RoomItem_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            OpenRoomFromSender(sender);
        }

        /// <summary>
        /// Обработчик клика по кнопке открытия комнаты
        /// </summary>
        private void OpenRoomButton_Click(object sender, RoutedEventArgs e)
        {
            OpenRoomFromSender(sender);
        }

        /// <summary>
        /// Общий метод для открытия комнаты
        /// </summary>
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