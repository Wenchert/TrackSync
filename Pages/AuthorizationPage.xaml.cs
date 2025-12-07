using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using TrackSync.DataBase.Model;
using TrackSync.DataBase.WorkWithDB;



namespace TrackSync.Pages
{
    public partial class AuthorizationPage : Page
    {
        private bool _isLoginMode = true;
        public static RoutedCommand Login = new RoutedCommand();
        public AuthorizationPage()
        {
            InitializeComponent();
            SetLoginMode(true);
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

        //Ввод - только цифры
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

        private void ErroreMessageVisible(string message)
        {
            ErrorMessage.Visibility = Visibility.Visible;
            ErrorMessage.Text = message;
        }
        private void CommandBindingExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (_isLoginMode)
            {
                if (ValidFieldLogin())
                {

                }
            }
            else
            {
                if (ValidFieldLogin() && ValidFieldSignUp()) 
                {
                    TrackSyncContext ctx = new TrackSyncContext();
                    ctx.Users.Add(new User(LoginText.Text, PasswordText.Password, Convert.ToInt32(WeightText.Text), Convert.ToInt32(HeightText.Text)));
                    ctx.SaveChanges();
                }

            }
        }

        private bool ValidFieldLogin()
        {
            if (string.IsNullOrEmpty(LoginText.Text) || string.IsNullOrEmpty(PasswordText.Password))
            {
                ErroreMessageVisible("*Не все поля заполнены");
                return false;
            }
            else if (IsExitLogin(LoginText.Text))
            {
                ErroreMessageVisible("*Пользователь с таким логином уже существует");
                return false;
            }
            else if (PasswordText.Password.Length < 8)
            {
                ErroreMessageVisible("*Пароль должен быть не меньше 8 символов");
                return false;
            }
            else
            {
                return true;
            }
        }

        private bool ValidFieldSignUp()
        {
            if(string.IsNullOrEmpty(WeightText.Text) || string.IsNullOrEmpty(HeightText.Text))
            {
                ErroreMessageVisible("*Все поля должны быть заполнены");
                return false; 
            }
            else if (Convert.ToInt16(HeightText.Text) < 40)
            {
                ErroreMessageVisible("*Ваш рост не может быть меньше 40 см.");
                return false;
            }
            return true;
        }

        public bool IsExitLogin(string login)
        {
            TrackSyncContext ctx = new TrackSyncContext();
            if(ctx.Users.Any(u => u.Name == login))
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}
