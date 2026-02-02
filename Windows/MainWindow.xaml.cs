using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using TrackSync.DataBase.WorkWithDB;
using TrackSync.Pages;
using static TrackSync.DataBase.WorkWithDB.TrackSyncManagerList;

namespace TrackSync.Windows
{
    public partial class MainWindow : Window
    {
        private bool isSidebarOpen = false;

        public MainWindow()
        {
            InitializeComponent();
            FrameContext.MainWindowFrame = mainWindow;
            mainWindow.Navigated += MainWindow_Navigated;
            LoadSidebarRooms();
            NavigateToAuthorization();
        }

        // Метод загрузки комнат в боковую панель
        private void LoadSidebarRooms()
        {
            try
            {
                var rooms = TrackSyncManagerList.GetAvailableRooms(SessionManager.CurrentUserId);
                SidebarRoomsListBox.ItemsSource = rooms;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки комнат: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Обработчики для боковой панели
        private void SidebarRoomItem_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            OpenRoomFromSidebar(sender);
        }

        private void SidebarOpenRoomButton_Click(object sender, RoutedEventArgs e)
        {
            OpenRoomFromSidebar(sender);
        }

        private void OpenRoomFromSidebar(object sender)
        {
            int roomId = 0;

            if (sender is Border border && border.Tag is int id1)
                roomId = id1;
            else if (sender is Button button && button.Tag is int id2)
                roomId = id2;

            if (roomId > 0)
            {
                isSidebarOpen = false;
                SidebarPanel.Visibility = Visibility.Collapsed;
                mainWindow.Navigate(new HikeRoomPage(roomId));
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
        }

        // Обработка навигации для скрытия/показа шапки и футера
        private void MainWindow_Navigated(object sender, NavigationEventArgs e)
        {
            // Скрываем шапку и футер только на странице авторизации/регистрации
            bool isAuthPage = e.Content is AuthorizationPage;

            AppHeader.Visibility = isAuthPage ? Visibility.Collapsed : Visibility.Visible;
            AppFooter.Visibility = isAuthPage ? Visibility.Collapsed : Visibility.Visible;
            SidebarPanel.Visibility = isAuthPage ? Visibility.Collapsed : (isSidebarOpen ? Visibility.Visible : Visibility.Collapsed);
        }

        // Навигация на страницу авторизации
        public void NavigateToAuthorization()
        {
            mainWindow.Navigate(new AuthorizationPage());
        }

        // Навигация на главную страницу (после успешной авторизации)
        public void NavigateToMainPage()
        {
            mainWindow.Navigate(new MainPage());
        }

        // === ЛОГИКА БОКОВОЙ ПАНЕЛИ ===
        private void SidebarToggleBtn_Click(object sender, RoutedEventArgs e)
        {
            isSidebarOpen = !isSidebarOpen;
            SidebarPanel.Visibility = isSidebarOpen ? Visibility.Visible : Visibility.Collapsed;
        }

        // === ОБРАБОТЧИКИ СОБЫТИЙ САЙДБАРА ===
        private void CreateRoomMenuBtn_Click(object sender, RoutedEventArgs e)
        {
            isSidebarOpen = false;
            SidebarPanel.Visibility = Visibility.Collapsed;
            mainWindow.Navigate(new CreateRoomPage());
        }

        private void RecipesCatalogBtn_Click(object sender, RoutedEventArgs e)
        {
            isSidebarOpen = false;
            SidebarPanel.Visibility = Visibility.Collapsed;
            FrameContext.MainWindowFrame.Navigate(new CatalogRecipesPage());
        }

        private void EquipmentCatalogBtn_Click(object sender, RoutedEventArgs e)
        {
            isSidebarOpen = false;
            SidebarPanel.Visibility = Visibility.Collapsed;
            FrameContext.MainWindowFrame.Navigate(new CatalogEquipmentsPage());
        }

        private void RoomItem_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            isSidebarOpen = false;
            SidebarPanel.Visibility = Visibility.Collapsed;

            var roomItem = sender as FrameworkElement;
            string roomId = roomItem?.Tag?.ToString();
            MessageBox.Show($"Открытие комнаты ID: {roomId}",
                          "TrackSync", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void RoomSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Логика поиска будет реализована позже
        }

        // === ОБРАБОТЧИКИ ХЕДЕРА ===
        private void NotificationsBtn_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Уведомления будут реализованы в будущих версиях",
                          "TrackSync", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ProfileBtn_Click(object sender, RoutedEventArgs e)
        {
            FrameContext.MainWindowFrame.Navigate(new ProfileSettingsPage());
        }

        // === ОБРАБОТЧИКИ ФУТЕРА ===
        private void VkBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "https://vk.com",
                    UseShellExecute = true
                });
            }
            catch
            {
                MessageBox.Show("Не удалось открыть ВКонтакте", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TelegramBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "https://telegram.org",
                    UseShellExecute = true
                });
            }
            catch
            {
                MessageBox.Show("Не удалось открыть Telegram", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Метод для обновления количества уведомлений
        public void UpdateNotificationCount(int count)
        {
            NotificationCount.Text = count.ToString();
            NotificationBadge.Visibility = count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}