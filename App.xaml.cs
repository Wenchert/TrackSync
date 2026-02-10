using PdfSharp.Fonts;
using System.Configuration;
using System.Data;
using System.Windows;
using TrackSync.Functions;

namespace TrackSync
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Инициализируем FontResolver для PdfSharp
            if (GlobalFontSettings.FontResolver == null)
            {
                GlobalFontSettings.FontResolver = new CustomFontResolver();
            }
        }

    }

}
