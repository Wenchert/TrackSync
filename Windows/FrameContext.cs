using System.Windows.Controls;

namespace TrackSync.Windows
{
    internal class FrameContext
    {
        public static Frame MainWindowFrame { get; set; }

        public static void NavigateToAuthorization()
        {
            if (MainWindowFrame?.Parent is MainWindow mainWindow)
            {
                mainWindow.NavigateToAuthorization();
            }
        }

        public static void NavigateToMainPage()
        {
            if (MainWindowFrame?.Parent is MainWindow mainWindow)
            {
                mainWindow.NavigateToMainPage();
            }
        }
    }
}