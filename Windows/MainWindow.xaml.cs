using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using TrackSync.DataBase;
using TrackSync.Pages;
using Microsoft.EntityFrameworkCore;
using TrackSync.DataBase.Model;
using TrackSync.DataBase.WorkWithDB;
using TrackSync.Functions;

namespace TrackSync.Windows
{
    public partial class MainWindow : Window
    {
        private bool isSidebarOpen = false;
        private List<HikeRoom> _allRooms;
        private int? _passwordRoomId = null;

        public MainWindow()
        {
            InitializeComponent();
            FrameContext.MainWindowFrame = mainWindow;
            mainWindow.Navigated += MainWindow_Navigated;
            NavigateToAuthorization();
        }

        // Метод загрузки комнат в боковую панель
        private void LoadSidebarRooms()
        {
            try
            {
                _allRooms = TrackSyncManagerList.GetAvailableRooms(SessionManager.CurrentUserId);
                SidebarRoomsListBox.ItemsSource = _allRooms;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки комнат: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Поиск комнат по названию
        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                string searchText = SearchTextBox.Text?.Trim().ToLower() ?? "";

                if (string.IsNullOrWhiteSpace(searchText))
                {
                    // Показываем все комнаты
                    SidebarRoomsListBox.ItemsSource = _allRooms;
                }
                else
                {
                    // Фильтруем по названию
                    var filteredRooms = _allRooms
                        .Where(room => room.Title?.ToLower().Contains(searchText) == true)
                        .ToList();
                    SidebarRoomsListBox.ItemsSource = filteredRooms;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка поиска: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Обработчик клика по комнате в боковой панели
        private void SidebarRoomItem_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is int roomId)
            {
                JoinRoom(roomId);
            }
        }

        // Логика присоединения к комнате
        private void JoinRoom(int roomId)
        {
            try
            {
                using var ctx = new TrackSyncContext();
                var room = ctx.HikeRooms
                    .Include(r => r.Status)
                    .FirstOrDefault(r => r.HikeRoomId == roomId);

                // КРИТИЧЕСКИ ВАЖНО: проверка на null
                if (room == null)
                {
                    MessageBox.Show("Комната не найдена", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Проверяем, является ли комната приватной
                if (!string.IsNullOrWhiteSpace(room.Password))
                {
                    ShowPasswordPanel(roomId);
                }
                else
                {
                    SendJoinRequest(room);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Показать поле ввода пароля под комнатой
        private void ShowPasswordPanel(int roomId)
        {
            HidePasswordPanel();

            // Обновляем макет, чтобы все контейнеры сгенерировались
            SidebarRoomsListBox.UpdateLayout();

            var passwordPanel = FindPasswordPanelForRoom(roomId);
            if (passwordPanel != null)
            {
                passwordPanel.Visibility = Visibility.Visible;
                _passwordRoomId = roomId;

                // Фокус на поле ввода пароля
                var passwordBox = FindChild<PasswordBox>(passwordPanel, "RoomPasswordBox");
                passwordBox?.Focus();
            }
        }

        // Скрыть все поля ввода пароля
        private void HidePasswordPanel()
        {
            if (_passwordRoomId.HasValue)
            {
                var panel = FindPasswordPanelForRoom(_passwordRoomId.Value);
                if (panel != null)
                {
                    panel.Visibility = Visibility.Collapsed;
                    var errorBlock = FindChild<TextBlock>(panel, "PasswordError");
                    if (errorBlock != null) errorBlock.Visibility = Visibility.Collapsed;
                }
                _passwordRoomId = null;
            }
        }

        // Найти панель пароля для конкретной комнаты (ИСПРАВЛЕННЫЙ МЕТОД)
        private StackPanel FindPasswordPanelForRoom(int roomId)
        {
            // Перебираем ВСЕ контейнеры в списке
            for (int i = 0; i < SidebarRoomsListBox.Items.Count; i++)
            {
                // Получаем контейнер элемента списка
                var container = SidebarRoomsListBox.ItemContainerGenerator.ContainerFromIndex(i) as ListBoxItem;
                if (container == null) continue;

                // Проверяем, что контейнер содержит нужную комнату
                if (container.Content is HikeRoom room && room.HikeRoomId == roomId)
                {
                    // Ищем панель пароля внутри контейнера
                    return FindChild<StackPanel>(container, "PasswordPanel");
                }
            }
            return null;
        }

        // Вспомогательный метод для поиска дочернего элемента в визуальном дереве
        private T FindChild<T>(DependencyObject parent, string childName) where T : DependencyObject
        {
            if (parent == null) return null;

            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T t && (string.IsNullOrEmpty(childName) || (child is FrameworkElement fe && fe.Name == childName)))
                {
                    return t;
                }

                var childOfChild = FindChild<T>(child, childName);
                if (childOfChild != null)
                    return childOfChild;
            }
            return null;
        }

        // Обработчик кнопки "Отменить" пароль
        private void CancelPassword_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.Tag is int roomId)
            {
                HidePasswordPanel();
            }
        }

        // Обработчик кнопки "Отправить" пароль
        private void SubmitPassword_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.Tag is int roomId)
            {
                // Находим панель пароля для этой комнаты
                var panel = FindPasswordPanelForRoom(roomId);
                if (panel == null) return;

                var passwordBox = FindChild<PasswordBox>(panel, "RoomPasswordBox");
                var errorBlock = FindChild<TextBlock>(panel, "PasswordError");

                if (passwordBox == null || errorBlock == null) return;

                string password = passwordBox.Password;

                if (string.IsNullOrWhiteSpace(password))
                {
                    errorBlock.Text = "*Введите пароль";
                    errorBlock.Visibility = Visibility.Visible;
                    return;
                }

                // Проверяем пароль
                if (VerifyRoomPassword(roomId, password))
                {
                    // Пароль верный - присоединяемся к комнате
                    if (AddUserToRoomDirectly(roomId))
                    {
                        // Успешное присоединение - скрываем панель и переходим в комнату
                        HidePasswordPanel();
                        isSidebarOpen = false;
                        SidebarPanel.Visibility = Visibility.Collapsed;
                        mainWindow.Navigate(new HikeRoomPage(roomId));
                        LoadSidebarRooms(); // Обновляем список комнат
                    }
                    else
                    {
                        errorBlock.Text = "*Ошибка при присоединении к комнате";
                        errorBlock.Visibility = Visibility.Visible;
                    }
                }
                else
                {
                    errorBlock.Text = "*Неверный пароль";
                    errorBlock.Visibility = Visibility.Visible;
                    passwordBox.Clear();
                    passwordBox.Focus();
                }
            }
        }

        // Проверка пароля комнаты
        private bool VerifyRoomPassword(int roomId, string password)
        {
            try
            {
                using var ctx = new TrackSyncContext();
                var room = ctx.HikeRooms.Find(roomId);

                if (room == null)
                {
                    return false;
                }

                if (string.IsNullOrWhiteSpace(room.Password))
                {
                    return false;
                }

                // Удаляем пробелы в начале и конце
                string trimmedPassword = password.Trim();

                bool result = BCrypt.Net.BCrypt.EnhancedVerify(trimmedPassword, room.Password);

                return result;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private bool AddUserToRoomDirectly(int roomId)
        {
            try
            {
                bool rulesParticipant = false;
                using var ctx = new TrackSyncContext();
                var participant = new Participant
                {
                    HikeRoomId = roomId,
                    UserId = SessionManager.CurrentUserId,
                    EditMealPlan = rulesParticipant,
                    EditTrack = rulesParticipant,
                    EditEquipment = rulesParticipant,

                };

                ctx.Participants.Add(participant);
                ctx.SaveChanges();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void SendJoinRequest(HikeRoom room)
        {
            try
            {
                // Проверяем, не отправлял ли пользователь уже заявку
                if (HasPendingRequest(room.HikeRoomId))
                {
                    MessageBox.Show("Вы уже отправили заявку на присоединение к этой комнате",
                                  "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Отправляем заявку
                using var ctx = new TrackSyncContext();
                var bid = new Bid
                {
                    HikeRoomId = room.HikeRoomId,
                    UserId = SessionManager.CurrentUserId,
                };

                ctx.Bids.Add(bid);
                ctx.SaveChanges();

                MessageBox.Show($"Заявка на присоединение к комнате '{room.Title}' отправлена.\n" +
                              $"Ожидайте подтверждения от администратора.",
                              "Заявка отправлена", MessageBoxButton.OK, MessageBoxImage.Information);

                // Обновляем список комнат
                LoadSidebarRooms();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка отправки заявки: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Проверка наличия ожидающей заявки
        private bool HasPendingRequest(int roomId)
        {
            try
            {
                using var ctx = new TrackSyncContext();
                return ctx.Bids.Any(b =>
                    b.HikeRoomId == roomId &&
                    b.UserId == SessionManager.CurrentUserId);
            }
            catch
            {
                return false;
            }
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
            LoadSidebarRooms(); // Обновляем список комнат
        }

        // === ЛОГИКА БОКОВОЙ ПАНЕЛИ ===
        private void SidebarToggleBtn_Click(object sender, RoutedEventArgs e)
        {
            LoadSidebarRooms();
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
            FrameContext.MainWindowFrame.Navigate(new CreateRecipePage());
        }

        private void EquipmentCatalogBtn_Click(object sender, RoutedEventArgs e)
        {
            isSidebarOpen = false;
            SidebarPanel.Visibility = Visibility.Collapsed;
            FrameContext.MainWindowFrame.Navigate(new CreateEquipmentPage());
        }

        // === ОБРАБОТЧИКИ ХЕДЕРА ===

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
                    FileName = "https://vk.com/ch_vova20",
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
                    FileName = "https://telegram.org/@Vovaa_22",
                    UseShellExecute = true
                });
            }
            catch
            {
                MessageBox.Show("Не удалось открыть Telegram", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}