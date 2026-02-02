using System.Windows;
using System.Windows.Controls;
using TrackSync.Windows;

namespace TrackSync.Pages
{
    public partial class LayoutPage : Page
    {
        private bool _isSidebarOpen = false;

        public LayoutPage()
        {
            InitializeComponent();
        }

        // Метод для установки контента дочерней страницы
        protected void SetContent(UIElement content)
        {
            PageContent.Content = content;
        }

        // === ЛОГИКА БОКОВОЙ ПАНЕЛИ ===
        private void SidebarToggleBtn_Click(object sender, RoutedEventArgs e)
        {
            _isSidebarOpen = !_isSidebarOpen;
            SidebarPanel.Visibility = _isSidebarOpen ? Visibility.Visible : Visibility.Collapsed;
        }

        // === ОБРАБОТЧИКИ СОБЫТИЙ САЙДБАРА ===
        private void CreateRoomMenuBtn_Click(object sender, RoutedEventArgs e)
        {
            _isSidebarOpen = false;
            //SidebarPanel.Visibility = Visibility.Collapsed;
            //FrameContext.MainWindowFrame.Navigate(new CreateRoomPage());
        }

        private void RecipesCatalogBtn_Click(object sender, RoutedEventArgs e)
        {
            _isSidebarOpen = false;
            SidebarPanel.Visibility = Visibility.Collapsed;
            MessageBox.Show("Каталог рецептов будет доступен в будущих версиях",
                          "TrackSync", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void EquipmentCatalogBtn_Click(object sender, RoutedEventArgs e)
        {
            _isSidebarOpen = false;
            SidebarPanel.Visibility = Visibility.Collapsed;
            MessageBox.Show("Каталог снаряжения будет доступен в будущих версиях",
                          "TrackSync", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void RoomItem_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _isSidebarOpen = false;
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
            MessageBox.Show("Профиль будет реализован в будущих версиях",
                          "TrackSync", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // === ОБРАБОТЧИКИ ФУТЕРА ===
        private void VkBtn_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://vk.com",
                UseShellExecute = true
            });
        }

        private void TelegramBtn_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://telegram.org",
                UseShellExecute = true
            });
        }

        public void UpdateNotificationCount(int count)
        {
            NotificationCount.Text = count.ToString();
            NotificationBadge.Visibility = count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}