using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

using TrackSync.DataBase;
using TrackSync.DataBase.Model;
using TrackSync.DataBase.WorkWithDB;
using TrackSync.Functions;
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

        private void LoadUserProfile()
        {
            try
            {
                using var ctx = new TrackSyncContext();
                int currentUserId = SessionManager.CurrentUserId;

                _currentUser = ctx.Users.Find(currentUserId);

                if (_currentUser != null)
                {
                    EmailTextBox.Text = _currentUser.Email;

                    if (!string.IsNullOrWhiteSpace(_currentUser.Name))
                    {
                        NameTextBox.Text = _currentUser.Name;
                    }

                    if (!string.IsNullOrWhiteSpace(_currentUser.Contacts))
                    {
                        ContactsTextBox.Text = _currentUser.Contacts;
                    }

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

        private void SaveProfileButton_Click(object sender, RoutedEventArgs e)
        {
            HideMessage(ErrorMessage);

            if (!ValidateName())
                return;

            if (!ValidateHeightWeight())
                return;

            if (!ValidateContacts())
                return;

            if (SaveProfileChanges())
            {
                ShowMessage(ErrorMessage, "*Настройки успешно сохранены!", true);
            }
        }

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

        private bool ValidateContacts()
        {

            if (ContactsTextBox.Text.Length > 50)
            {
                ShowMessage(ErrorMessage, "*Поле контктов должно содержать не больше 50 символов", false);
                return false;
            }

            return true;
        }

        private bool SaveProfileChanges()
        {
            try
            {
                using var ctx = new TrackSyncContext();
                var user = ctx.Users.Find(_currentUser.UserId);

                if (user == null)
                    throw new Exception("Пользователь не найден");

                user.Name = NameTextBox.Text.Trim();

                string contacts = ContactsTextBox.Text.Trim();
                user.Contacts = string.IsNullOrWhiteSpace(contacts) ? null : contacts;

                user.Height = int.Parse(HeightText.Text);
                user.Weight = int.Parse(WeightText.Text);

                ctx.SaveChanges();

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

        private void ChangePasswordButton_Click(object sender, RoutedEventArgs e)
        {
            HideMessage(PasswordMessage);

            if (!ValidatePasswordChange())
                return;

            if (!VerifyCurrentPassword())
                return;

            if (ChangeUserPassword(NewPasswordBox.Password))
            {
                ShowMessage(PasswordMessage, "*Пароль успешно изменён!", true);

                CurrentPasswordBox.Clear();
                NewPasswordBox.Clear();
                ConfirmPasswordBox.Clear();
            }
        }

        private bool ValidateHeightWeight()
        {
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

        private bool ChangeUserPassword(string newPassword)
        {
            try
            {
                using var ctx = new TrackSyncContext();
                var user = ctx.Users.Find(_currentUser.UserId);

                if (user == null)
                    throw new Exception("Пользователь не найден");

                user.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);

                ctx.SaveChanges();

                _currentUser = user;
                return true;
            }
            catch (Exception ex)
            {
                ShowMessage(PasswordMessage, $"*Ошибка изменения пароля: {ex.Message}", false);
                return false;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService.CanGoBack)
                NavigationService.GoBack();
        }

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