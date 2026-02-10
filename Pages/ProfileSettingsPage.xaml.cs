using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using TrackSync.DataBase;
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
                int currentUserId = SessionManager.CurrentUserId;

                _currentUser = ctx.Users.Find(currentUserId);

                if (_currentUser != null)
                {
                    // Загружаем почту (только для чтения)
                    EmailTextBox.Text = _currentUser.Email;

                    // Загружаем имя
                    if (!string.IsNullOrWhiteSpace(_currentUser.Name))
                    {
                        NameTextBox.Text = _currentUser.Name;
                    }

                    // Загружаем контакты
                    if (!string.IsNullOrWhiteSpace(_currentUser.Contacts))
                    {
                        ContactsTextBox.Text = _currentUser.Contacts;
                    }

                    // Загружаем рост и вес
                    HeightText.Text = _currentUser.Height.ToString();
                    WeightText.Text = _currentUser.Weight.ToString();
                }
                else
                {
                    ShowMessage(ErrorMessage, "*Не удалось загрузить данные профиля", false);
                }
            }
            catch (Exception ex)
            {
                ShowMessage(ErrorMessage, $"*Ошибка при загрузке профиля: {ex.Message}", false);
            }
        }

        // Сохранение изменений профиля (вкладка "Профиль")
        private void SaveProfileButton_Click(object sender, RoutedEventArgs e)
        {
            HideMessage(ErrorMessage);

            // Валидация имени
            if (!ValidateName())
                return;

            // Валидация роста и веса
            if (!ValidateHeightWeight())
                return;

            // Сохраняем изменения
            if (SaveProfileChanges())
            {
                ShowMessage(ErrorMessage, "*Настройки успешно сохранены!", true);
            }
        }

        // Валидация имени
        private bool ValidateName()
        {
            if (string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                ShowMessage(ErrorMessage, "*Поле имени должно быть заполнено", false);
                return false;
            }

            if (NameTextBox.Text.Length > 30)
            {
                ShowMessage(ErrorMessage, "*Поле имени должно содержать не больше 30 символов", false);
                return false;
            }

            return true;
        }

        // Сохранение изменений профиля
        private bool SaveProfileChanges()
        {
            try
            {
                using var ctx = new TrackSyncContext();
                var user = ctx.Users.Find(_currentUser.UserId);

                if (user == null)
                    throw new Exception("Пользователь не найден");

                // Сохраняем имя
                user.Name = NameTextBox.Text.Trim();

                // Сохраняем контакты
                string contacts = ContactsTextBox.Text.Trim();
                user.Contacts = string.IsNullOrWhiteSpace(contacts) ? null : contacts;

                // Сохраняем рост и вес
                user.Height = int.Parse(HeightText.Text);
                user.Weight = int.Parse(WeightText.Text);

                ctx.SaveChanges();

                // Обновляем локальную копию
                _currentUser = user;
                return true;
            }
            catch (Exception ex)
            {
                ShowMessage(ErrorMessage, $"*Ошибка сохранения: {ex.Message}", false);
                return false;
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
            HideMessage(ErrorMessage);
        }

        // Изменение пароля (вкладка "Безопасность")
        private void ChangePasswordButton_Click(object sender, RoutedEventArgs e)
        {
            HideMessage(PasswordMessage);

            // Валидация полей пароля
            if (!ValidatePasswordChange())
                return;

            // Проверка текущего пароля
            if (!VerifyCurrentPassword())
                return;

            // Сохраняем новый пароль
            if (ChangeUserPassword(NewPasswordBox.Password))
            {
                ShowMessage(PasswordMessage, "*Пароль успешно изменён!", true);

                // Очищаем поля
                CurrentPasswordBox.Clear();
                NewPasswordBox.Clear();
                ConfirmPasswordBox.Clear();
            }
        }

        // Валидация роста и веса
        private bool ValidateHeightWeight()
        {
            // Проверка роста
            if (string.IsNullOrWhiteSpace(HeightText.Text))
            {
                ShowMessage(ErrorMessage, "*Пожалуйста, укажите ваш рост", false);
                return false;
            }

            if (!int.TryParse(HeightText.Text, out int height) || height < 40)
            {
                ShowMessage(ErrorMessage, "*Рост должен быть не менее 40 см", false);
                return false;
            }

            // Проверка веса
            if (string.IsNullOrWhiteSpace(WeightText.Text))
            {
                ShowMessage(ErrorMessage, "*Пожалуйста, укажите ваш вес", false);
                return false;
            }

            if (!int.TryParse(WeightText.Text, out int weight) || weight < 20)
            {
                ShowMessage(ErrorMessage, "*Вес должен быть не менее 20 кг", false);
                return false;
            }

            return true;
        }

        // Валидация полей пароля
        private bool ValidatePasswordChange()
        {
            if (string.IsNullOrWhiteSpace(CurrentPasswordBox.Password))
            {
                ShowMessage(PasswordMessage, "*Введите текущий пароль", false);
                return false;
            }

            if (string.IsNullOrWhiteSpace(NewPasswordBox.Password))
            {
                ShowMessage(PasswordMessage, "*Введите новый пароль", false);
                return false;
            }

            if (NewPasswordBox.Password.Length < 8)
            {
                ShowMessage(PasswordMessage, "*Новый пароль должен содержать не менее 8 символов", false);
                return false;
            }

            if (NewPasswordBox.Password != ConfirmPasswordBox.Password)
            {
                ShowMessage(PasswordMessage, "*Пароли не совпадают", false);
                return false;
            }

            return true;
        }

        // Проверка текущего пароля
        private bool VerifyCurrentPassword()
        {
            try
            {
                using var ctx = new TrackSyncContext();
                var user = ctx.Users.Find(_currentUser.UserId);

                if (user == null || !BCrypt.Net.BCrypt.Verify(CurrentPasswordBox.Password, user.Password))
                {
                    ShowMessage(PasswordMessage, "*Неверный текущий пароль", false);
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                ShowMessage(PasswordMessage, $"*Ошибка проверки пароля: {ex.Message}", false);
                return false;
            }
        }

        // Изменение пароля пользователя
        private bool ChangeUserPassword(string newPassword)
        {
            try
            {
                using var ctx = new TrackSyncContext();
                var user = ctx.Users.Find(_currentUser.UserId);

                if (user == null)
                    throw new Exception("Пользователь не найден");

                // Хешируем новый пароль
                user.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);

                ctx.SaveChanges();

                // Обновляем локальную копию
                _currentUser = user;
                return true;
            }
            catch (Exception ex)
            {
                ShowMessage(PasswordMessage, $"*Ошибка изменения пароля: {ex.Message}", false);
                return false;
            }
        }

        // Отмена изменений
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            FrameContext.MainWindowFrame.Navigate(new MainPage());
        }

        // Универсальные методы для работы с сообщениями
        private void ShowMessage(TextBlock messageBlock, string message, bool isSuccess)
        {
            messageBlock.Visibility = Visibility.Visible;
            messageBlock.Text = message;
            messageBlock.Foreground = isSuccess ?
                (System.Windows.Media.Brushes.Green) :
                (System.Windows.Media.Brushes.Red);

            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            timer.Tick += (s, args) =>
            {
                messageBlock.Visibility = Visibility.Collapsed;
                timer.Stop();
            };
            timer.Start();
        }

        private void HideMessage(TextBlock messageBlock)
        {
            messageBlock.Visibility = Visibility.Collapsed;
        }

        // Обработчик кнопки выхода
        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SessionManager.ClearSession();

                if (FrameContext.MainWindowFrame != null)
                {
                    FrameContext.MainWindowFrame.Navigate(new AuthorizationPage());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Ошибка при выходе из системы: {ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }
    }
}