using System.Windows.Controls;

namespace TrackSync.Windows
{
    internal class FrameContext
    {
        public static Frame MainWindowFrame { get; set; }

        // Утилитный метод для навигации на авторизацию
        public static void NavigateToAuthorization()
        {
            if (MainWindowFrame?.Parent is MainWindow mainWindow)
            {
                mainWindow.NavigateToAuthorization();
            }
        }

        // Утилитный метод для навигации на главную
        public static void NavigateToMainPage()
        {
            if (MainWindowFrame?.Parent is MainWindow mainWindow)
            {
                mainWindow.NavigateToMainPage();
            }
        }
    }
}