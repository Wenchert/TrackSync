using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TrackSync.DataBase;
//using TrackSync.DataBase.Model;
using TrackSync.DataBase.WorkWithDB;
using TrackSync.Windows;

namespace TrackSync.Pages
{
    public partial class ProfileSettingsPage : Page
    {
        private User _currentUser;

        public ProfileSettingsPage()
        {
            InitializeComponent();
            LoadUserProfile();
        }

        // Загрузка данных текущего пользователя
        private void LoadUserProfile()
        {
            try
            {
                using var ctx = new TrackSyncContext();
                int currentUserId = GetCurrentUserId();

                _currentUser = ctx.Users.Find(currentUserId);

                if (_currentUser != null)
                {
                    // Загружаем описание профиля
                    if (!string.IsNullOrWhiteSpace(_currentUser.Contacts))
                    {
                        ProfileDescriptionTextBox.Text = _currentUser.Contacts;
                    }

                    // Загружаем рост и вес
                    HeightText.Text = _currentUser.Height.ToString();
                    WeightText.Text = _currentUser.Weight.ToString();
                }
                else
                {
                    ErrorMessageVisible("*Не удалось загрузить данные профиля");
                }
            }
            catch (Exception ex)
            {
                ErrorMessageVisible($"*Ошибка при загрузке профиля: {ex.Message}");
            }
        }

        // Обработчики для кнопок роста
        private void HeightIncreaseBtn_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(HeightText.Text, out int height))
            {
                HeightText.Text = (height + 1).ToString();
                ErrorMessageHide();
            }
        }

        private void HeightDecreaseBtn_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(HeightText.Text, out int height) && height > 40)
            {
                HeightText.Text = (height - 1).ToString();
                ErrorMessageHide();
            }
        }

        // Обработчики для кнопок веса
        private void WeightIncreaseBtn_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(WeightText.Text, out int weight))
            {
                WeightText.Text = (weight + 1).ToString();
                ErrorMessageHide();
            }
        }

        private void WeightDecreaseBtn_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(WeightText.Text, out int weight) && weight > 20)
            {
                WeightText.Text = (weight - 1).ToString();
                ErrorMessageHide();
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
            ErrorMessageHide();
        }

        // Обработчик для скрытия ошибок при вводе описания
        private void ProfileDescriptionTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ErrorMessageHide();
        }

        // Обработчик кнопки "Изменить пароль через почту"
        private void ChangePasswordButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorMessageHide();

            if (_currentUser == null || string.IsNullOrWhiteSpace(_currentUser.Name))
            {
                ErrorMessageVisible("*Не удалось определить ваш email для отправки письма");
                return;
            }

            try
            {
                // Здесь будет логика отправки письма (временно заглушка)
                System.Threading.Thread.Sleep(800); // Имитация задержки

                // Показываем сообщение об успешной отправке
                PasswordChangeMessage.Visibility = Visibility.Visible;
                ErrorMessageVisible($"✓ Инструкции отправлены на {MaskEmail(_currentUser.Name)}");

                // Скрываем сообщение через 5 секунд
                var timer = new System.Windows.Threading.DispatcherTimer();
                timer.Interval = TimeSpan.FromSeconds(5);
                timer.Tick += (s, args) =>
                {
                    PasswordChangeMessage.Visibility = Visibility.Collapsed;
                    timer.Stop();
                };
                timer.Start();
            }
            catch (Exception ex)
            {
                ErrorMessageVisible($"*Ошибка при отправке письма: {ex.Message}");
            }
        }

        // Маскирование email для отображения
        private string MaskEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
                return email;

            var parts = email.Split('@');
            if (parts.Length != 2)
                return email;

            var localPart = parts[0];
            if (localPart.Length <= 2)
                return email;

            var maskedLocal = localPart.Substring(0, 2) + new string('*', localPart.Length - 2);
            return $"{maskedLocal}@{parts[1]}";
        }

        // Сохранение изменений
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorMessageHide();

            // Валидация роста и веса
            if (!ValidateHeightWeight())
                return;

            // Сохраняем изменения
            SaveProfileChanges();

            MessageBox.Show("Настройки успешно сохранены!",
                          "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

            // Возвращаемся на главную страницу
            FrameContext.MainWindowFrame.Navigate(new MainPage());
        }

        // Валидация роста и веса
        private bool ValidateHeightWeight()
        {
            // Проверка роста
            if (string.IsNullOrWhiteSpace(HeightText.Text))
            {
                ErrorMessageVisible("*Пожалуйста, укажите ваш рост");
                return false;
            }

            if (!int.TryParse(HeightText.Text, out int height) || height < 40)
            {
                ErrorMessageVisible("*Рост должен быть не менее 40 см");
                return false;
            }

            // Проверка веса
            if (string.IsNullOrWhiteSpace(WeightText.Text))
            {
                ErrorMessageVisible("*Пожалуйста, укажите ваш вес");
                return false;
            }

            if (!int.TryParse(WeightText.Text, out int weight) || weight < 20)
            {
                ErrorMessageVisible("*Вес должен быть не менее 20 кг");
                return false;
            }

            return true;
        }

        // Сохранение изменений профиля
        private void SaveProfileChanges()
        {
            try
            {
                using var ctx = new TrackSyncContext();
                var user = ctx.Users.Find(_currentUser.UserId);

                if (user != null)
                {
                    // Сохраняем описание
                    string description = ProfileDescriptionTextBox.Text.Trim();
                    user.Contacts = string.IsNullOrWhiteSpace(description) ? null : description;

                    // Сохраняем рост и вес
                    user.Height = int.Parse(HeightText.Text);
                    user.Weight = int.Parse(WeightText.Text);

                    ctx.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                // Не критичная ошибка - просто покажем сообщение
                MessageBox.Show($"Не удалось сохранить изменения: {ex.Message}",
                              "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // Отмена изменений
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            FrameContext.MainWindowFrame.Navigate(new MainPage());
        }

        // Методы для работы с сообщением об ошибке
        private void ErrorMessageVisible(string message)
        {
            ErrorMessage.Visibility = Visibility.Visible;
            ErrorMessage.Text = message;
        }

        private void ErrorMessageHide()
        {
            ErrorMessage.Visibility = Visibility.Collapsed;
        }

        // Метод для получения текущего пользователя
        private int GetCurrentUserId()
        {
            // Здесь нужно получить ID текущего авторизованного пользователя
            // Пока возвращаем заглушку
            return 1;
        }
    }
}