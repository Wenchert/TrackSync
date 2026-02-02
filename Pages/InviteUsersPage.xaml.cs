using System.Windows.Controls;
using TrackSync.Pages;

namespace TrackSync.Pages
{
    public partial class InviteUsersPage : Page
    {
        public InviteUsersPage()
        {
            InitializeComponent();
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Логика поиска будет добавлена позже
        }

        private void CancelButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (NavigationService.CanGoBack)
                NavigationService.GoBack();
        }

        private void AddUsersButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            // Логика добавления выбранных пользователей
            // (пока не реализовано)

            // Возврат на предыдущую страницу
            if (NavigationService.CanGoBack)
                NavigationService.GoBack();
        }
    }
}