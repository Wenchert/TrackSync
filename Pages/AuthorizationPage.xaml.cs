using BCrypt.Net;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using TrackSync.DataBase;
//using TrackSync.DataBase.Model;
using TrackSync.DataBase.WorkWithDB;
using TrackSync.Functions;
using TrackSync.Pages;
using TrackSync.Windows;

namespace TrackSync.Pages
{
    public partial class AuthorizationPage : Page
    {
        private bool _isLoginMode = true;
        public static RoutedCommand Login = new RoutedCommand();
        private DispatcherTimer _timer;
        private DateTime _unlockTime;
        private string _currentIpAddress;

        public AuthorizationPage()
        {
            InitializeComponent();
            SetLoginMode(true);
            LoadIpAddressAsync();
            CheckIpBlockStatusAsync();
        }

        private async void LoadIpAddressAsync()
        {
            try
            {
                _currentIpAddress = await NetworkHelper.GetExternalIpAddressAsync();
            }
            catch (Exception ex)
            {
                _currentIpAddress = "127.0.0.1";
            }
        }
        private async void CheckIpBlockStatusAsync()
        {
            _currentIpAddress = await NetworkHelper.GetExternalIpAddressAsync();

            using var ctx = new TrackSyncContext();

            // Чистим старые записи
            CleanOldLoginAttempts(ctx, _currentIpAddress);

            BlockUser(ctx, _currentIpAddress);

            return;
        }

        private void OnLoginMouseDown(object sender, MouseButtonEventArgs e)
        {
            SetLoginMode(true);
        }

        private void OnRegisterMouseDown(object sender, MouseButtonEventArgs e)
        {
            SetLoginMode(false);
        }

        private void SetLoginMode(bool isLogin)
        {
            _isLoginMode = isLogin;

            // Обновление цветов заголовков
            LoginTitle.Foreground = isLogin ? new SolidColorBrush(Color.FromRgb(52, 152, 219)) : new SolidColorBrush(Color.FromRgb(149, 165, 166));
            RegisterTitle.Foreground = isLogin ? new SolidColorBrush(Color.FromRgb(149, 165, 166)) : new SolidColorBrush(Color.FromRgb(52, 152, 219));

            // Показ/скрытие дополнительных полей
            HeightWeightPanel.Visibility = isLogin ? Visibility.Collapsed : Visibility.Visible;

            // Обновление текста кнопки
            AuthButton.Content = isLogin ? "Войти" : "Зарегистрироваться";

            // Очистка сообщений об ошибках при переключении
            ErrorMessage.Visibility = Visibility.Collapsed;

            // Если панель блокировки видна, скрываем поля ввода
            if (IpBlockPanel.Visibility == Visibility.Visible)
            {
                LoginPanel.Visibility = Visibility.Collapsed;
                PasswordPanel.Visibility = Visibility.Collapsed;
                AuthButton.Visibility = Visibility.Collapsed;
                if (!_isLoginMode)
                    HeightWeightPanel.Visibility = Visibility.Collapsed;
            }
            else
            {
                LoginPanel.Visibility = Visibility.Visible;
                PasswordPanel.Visibility = Visibility.Visible;
                AuthButton.Visibility = Visibility.Visible;
                if (!_isLoginMode)
                    HeightWeightPanel.Visibility = Visibility.Visible;
            }
        }

        // Обработчики для кнопок роста
        private void HeightIncreaseBtn_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(HeightText.Text, out int height))
            {
                HeightText.Text = (height + 1).ToString();
            }
        }

        private void HeightDecreaseBtn_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(HeightText.Text, out int height) && height > 1)
            {
                HeightText.Text = (height - 1).ToString();
            }
        }

        // Обработчики для кнопок веса
        private void WeightIncreaseBtn_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(WeightText.Text, out int weight))
            {
                WeightText.Text = (weight + 1).ToString();
            }
        }

        private void WeightDecreaseBtn_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(WeightText.Text, out int weight) && weight > 1)
            {
                WeightText.Text = (weight - 1).ToString();
            }
        }

        // Ввод - только цифры
        private void NumberPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            foreach (char c in e.Text)
            {
                if (!char.IsDigit(c))
                {
                    e.Handled = true;
                    break;
                }
            }
        }

        private void ErrorMessageVisible(string message)
        {
            ErrorMessage.Visibility = Visibility.Visible;
            ErrorMessage.Text = message;
        }

        private void ErrorMessageHide()
        {
            ErrorMessage.Visibility = Visibility.Collapsed;
        }

        private async void CommandBindingExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            ErrorMessageHide();

            if (_isLoginMode)
            {
                // ЛОГИНИЗАЦИЯ
                if (ValidateLoginFields())
                {
                    string ipAddress = await NetworkHelper.GetExternalIpAddressAsync();

                    using var ctx = new TrackSyncContext();

                    //Чистим старые записи
                    CleanOldLoginAttempts(ctx, ipAddress);

                    BlockUser(ctx, ipAddress);

                    var user = ctx.Users.FirstOrDefault(u => u.Name == LoginText.Text);

                    // Проверяем пароль через BCrypt и существование логина в БД
                    if (BCrypt.Net.BCrypt.Verify(PasswordText.Password, user.Password) || user == null)
                    {
                        // Успешный вход
                        RecordLoginAttempt(ctx, ipAddress, LoginText.Text, true);

                        // Создание сессии
                        SessionManager.CurrentUser = ctx.Users.FirstOrDefault(u => u.Name == LoginText.Text);

                        FrameContext.MainWindowFrame.Navigate(new MainPage());
                    }
                    else
                    {
                        // Неудачная попытка
                        RecordLoginAttempt(ctx, ipAddress, LoginText.Text, false);

                        BlockUser(ctx, ipAddress);

                        ErrorMessageVisible($"*Неверный логин или пароль");
                    }
                }
            }
            else
            {
                // РЕГИСТРАЦИЯ 
                if (!ValidateLoginFields() || !ValidateSignUpFields())
                    return;

                if (IsUserExists(LoginText.Text))
                {
                    ErrorMessageVisible("*Пользователь с таким логином уже существует");
                    return;
                }

                using var ctx = new TrackSyncContext();

                // Создаём нового пользователя
                var newUser = new User(
                    password: PasswordText.Password,
                    name: LoginText.Text,
                    weight: int.Parse(WeightText.Text),
                    height: int.Parse(HeightText.Text)
                );

                ctx.Users.Add(newUser);
                ctx.SaveChanges();

                // Создание сессии
                SessionManager.CurrentUser = newUser;

                // Успешная регистрация — переход на главную
                FrameContext.MainWindowFrame.Navigate(new MainPage());
            }
        }


        private void BlockUser(TrackSyncContext ctx, string ipAddress)
        {
            // Проверяем блокировку по IP
            int recentFailedAttempts = ctx.Loginattempts
                .Count(a => a.IpAddress == ipAddress &&
                           !a.Success &&
                           a.AttemptTime > DateTime.Now.AddMinutes(-15));

            if (recentFailedAttempts >= 5)
            {
                var lastFailedAttempt = ctx.Loginattempts
                    .Where(a => a.IpAddress == ipAddress && !a.Success)
                    .OrderByDescending(a => a.AttemptTime)
                    .First();

                _unlockTime = lastFailedAttempt.AttemptTime.AddMinutes(15);

                // Если время блокировки ещё не истекло
                if (DateTime.Now < _unlockTime)
                {
                    ShowIpBlockPanel();
                    return;
                }
            }
        }
        private void RecordLoginAttempt(TrackSyncContext ctx, string ipAddress, string email, bool success)
        {
            ctx.Loginattempts.Add(new Loginattempt
            {
                IpAddress = ipAddress,
                Email = email,
                AttemptTime = DateTime.Now,
                Success = success
            });
            ctx.SaveChanges();
        }
        private void CleanOldLoginAttempts(TrackSyncContext ctx, string currentUserIp)
        {
            var cutoffDate = DateTime.Now.AddMinutes(-15);

            // Находим время последней попытки для IP текущего пользователя
            var lastAttemptTime = ctx.Loginattempts
                .Where(a => a.IpAddress == currentUserIp)
                .Max(a => (DateTime?)a.AttemptTime);

            // Если последняя попытка была более 15 минут назад (или её не было)
            if (lastAttemptTime == null || lastAttemptTime < cutoffDate)
            {
                // Удаляем ВСЕ записи для этого IP
                var attemptsToDelete = ctx.Loginattempts
                    .Where(a => a.IpAddress == currentUserIp);

                ctx.Loginattempts.RemoveRange(attemptsToDelete);
                ctx.SaveChanges();
            }
        }

        private void ShowIpBlockPanel()
        {
            // Скрываем поля ввода
            LoginPanel.Visibility = Visibility.Collapsed;
            PasswordPanel.Visibility = Visibility.Collapsed;
            AuthButton.Visibility = Visibility.Collapsed;
            HeightWeightPanel.Visibility = Visibility.Collapsed;
            LoginTitle.Visibility = Visibility.Collapsed;
            RegisterTitle.Visibility = Visibility.Collapsed;

            // Показываем панель блокировки
            IpBlockPanel.Visibility = Visibility.Visible;

            // Запускаем таймер
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += Timer_Tick;
            _timer.Start();

            UpdateTimerDisplay();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            UpdateTimerDisplay();

            if (DateTime.Now >= _unlockTime)
            {
                _timer.Stop();
                HideIpBlockPanel();
            }
        }

        private void UpdateTimerDisplay()
        {
            TimeSpan remaining = _unlockTime - DateTime.Now;
            if (remaining.TotalSeconds < 0) remaining = TimeSpan.Zero;

            TimerText.Text = $"{remaining.Minutes:D2}:{remaining.Seconds:D2}";
        }

        private void HideIpBlockPanel()
        {
            IpBlockPanel.Visibility = Visibility.Collapsed;
            LoginTitle.Visibility = Visibility.Visible;
            RegisterTitle.Visibility = Visibility.Visible;
            SetLoginMode(true); // Используем общий метод для показа полей

            // Очищаем поля
            LoginText.Text = string.Empty;
            PasswordText.Password = string.Empty;
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            if (DateTime.Now >= _unlockTime)
            {
                _timer?.Stop();
                HideIpBlockPanel();
            }
            else
            {
                UpdateTimerDisplay();
            }
        }

        private bool ValidateLoginFields()
        {
            if (string.IsNullOrWhiteSpace(LoginText.Text) || string.IsNullOrEmpty(PasswordText.Password))
            {
                ErrorMessageVisible("*Все поля должны быть заполнены");
                return false;
            }

            if (!ValidEmail(LoginText.Text))
            {
                ErrorMessageVisible("*Некорректный формат email (должен содержать @ и точку)");
                return false;
            }

            if (PasswordText.Password.Length < 8)
            {
                ErrorMessageVisible("*Пароль должен содержать не менее 8 символов");
                return false;
            }

            return true;
        }

        private bool ValidateSignUpFields()
        {
            if (string.IsNullOrEmpty(WeightText.Text) || string.IsNullOrEmpty(HeightText.Text))
            {
                ErrorMessageVisible("*Все поля должны быть заполнены");
                return false;
            }

            if (!int.TryParse(HeightText.Text, out int height) || height < 40)
            {
                ErrorMessageVisible("*Рост должен быть не менее 40 см");
                return false;
            }

            if (!int.TryParse(WeightText.Text, out int weight) || weight < 20)
            {
                ErrorMessageVisible("*Вес должен быть не менее 20 кг");
                return false;
            }

            return true;
        }

        private bool ValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email && email.Contains("@") && email.Contains(".");
            }
            catch
            {
                return false;
            }
        }

        public bool IsUserExists(string login)
        {
            using (var ctx = new TrackSyncContext())
            {
                return ctx.Users.Any(u => u.Name == login);
            }
        }
    }
}