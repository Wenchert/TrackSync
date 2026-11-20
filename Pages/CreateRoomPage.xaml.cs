using System;
using System.Windows;
using System.Windows.Controls;
using TrackSync.DataBase.Model;
using TrackSync.DataBase.WorkWithDB;
using TrackSync.Functions;
using TrackSync.Windows;

namespace TrackSync.Pages
{
    public partial class CreateRoomPage : Page
    {
        public CreateRoomPage()
        {
            InitializeComponent();

            InitializeDefaultValues();
        }

        // Инициализация начальных значений
        private void InitializeDefaultValues()
        {
            // Устанавливаем даты по умолчанию
            StartDatePicker.SelectedDate = DateTime.Today;
            EndDatePicker.SelectedDate = DateTime.Today.AddDays(7);

            if (PublicRoomRadio != null)
            {
                PublicRoomRadio.IsChecked = true;
            }

            if (PasswordPanel != null)
            {
                PasswordPanel.Visibility = Visibility.Collapsed;
            }
        }

        // Обработчик изменения типа комнаты
        private void RoomTypeRadio_Checked(object sender, RoutedEventArgs e)
        {
            if (PrivateRoomRadio == null || PublicRoomRadio == null || PasswordPanel == null)
                return;

            PasswordPanel.Visibility = PrivateRoomRadio.IsChecked == true
                ? Visibility.Visible
                : Visibility.Collapsed;

            ErrorMessageHide();
        }

        private void TitleTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ErrorMessageHide();
        }

        private void DescriptionTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ErrorMessageHide();
        }

        // Валидация даты начала
        private void StartDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            ErrorMessageHide();

            if (StartDatePicker?.SelectedDate.HasValue == true && EndDatePicker?.SelectedDate.HasValue == true)
            {
                if (StartDatePicker.SelectedDate.Value > EndDatePicker.SelectedDate.Value)
                {
                    ErrorMessageVisible("*Дата начала не может быть позже даты окончания!");
                }
            }
        }

        // Валидация даты окончания
        private void EndDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            ErrorMessageHide();

            if (StartDatePicker?.SelectedDate.HasValue == true && EndDatePicker?.SelectedDate.HasValue == true)
            {
                if (EndDatePicker.SelectedDate.Value < StartDatePicker.SelectedDate.Value)
                {
                    ErrorMessageVisible("*Дата окончания не может быть раньше даты начала!");
                }
            }
        }

        // Создание комнаты
        private void CreateRoomButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorMessageHide();

            if (!ValidateForm())
                return;

            try
            {
                using var ctx = new TrackSyncContext();

                // Хэшируем пароль, если комната приватная
                string? passwordHash = null;
                if (PrivateRoomRadio.IsChecked.Value && !string.IsNullOrWhiteSpace(PasswordBox.Password))
                {
                    passwordHash = BCrypt.Net.BCrypt.EnhancedHashPassword(PasswordBox.Password);
                }

                var newRoom = new HikeRoom(
                    statusId: 1,
                    accessType: PublicRoomRadio.IsChecked.Value,
                    title: TitleTextBox.Text.Trim(),
                    startDate: DateOnly.FromDateTime(StartDatePicker.SelectedDate.Value.Date),
                    endDate: DateOnly.FromDateTime(EndDatePicker.SelectedDate.Value.Date),
                    leaderId: SessionManager.CurrentUserId,
                    password: passwordHash,
                    description: string.IsNullOrWhiteSpace(DescriptionTextBox.Text)
                        ? null
                        : DescriptionTextBox.Text.Trim()
                );

                ctx.HikeRooms.Add(newRoom);
                ctx.SaveChanges();

                var participant = new Participant(
                    userId: SessionManager.CurrentUserId,
                    hikeRoomId: newRoom.HikeRoomId,
                    editMealPlan: true,
                    editTrack: true,
                    editEquipment: true
                );

                ctx.Participants.Add(participant);
                ctx.SaveChanges();

                FrameContext.MainWindowFrame.Navigate(new HikeRoomPage(newRoom.HikeRoomId));
            }
            catch (Exception ex)
            {
                ErrorMessageVisible($"*Ошибка при создании комнаты: {ex.Message}");
            }
        }

        // Отмена создания
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (FrameContext.MainWindowFrame.CanGoBack)
            {
                FrameContext.MainWindowFrame.GoBack();
            }
            else
            {
                FrameContext.MainWindowFrame.Navigate(new MainPage());
            }
        }

        // Валидация формы
        private bool ValidateForm()
        {
            if (string.IsNullOrWhiteSpace(TitleTextBox?.Text))
            {
                ErrorMessageVisible("*Пожалуйста, введите название комнаты");
                return false;
            }

            if (TitleTextBox.Text.Length < 3)
            {
                ErrorMessageVisible("*Название комнаты должно содержать минимум 3 символа");
                return false;
            }

            // Проверка дат
            if (!StartDatePicker?.SelectedDate.HasValue == true)
            {
                ErrorMessageVisible("*Пожалуйста, выберите дату начала похода");
                return false;
            }

            if (!EndDatePicker?.SelectedDate.HasValue == true)
            {
                ErrorMessageVisible("*Пожалуйста, выберите дату окончания похода");
                return false;
            }

            // Проверка, что дата начала не в прошлом
            if (StartDatePicker.SelectedDate.Value < DateTime.Today)
            {
                ErrorMessageVisible("*Дата начала не может быть в прошлом!");
                return false;
            }

            // Проверка, что дата окончания не раньше начала
            if (EndDatePicker.SelectedDate.Value < StartDatePicker.SelectedDate.Value)
            {
                ErrorMessageVisible("*Дата окончания не может быть раньше даты начала!");
                return false;
            }

            // Проверка пароля для приватной комнаты
            if (PrivateRoomRadio?.IsChecked == true)
            {
                if (string.IsNullOrWhiteSpace(PasswordBox?.Password))
                {
                    ErrorMessageVisible("*Для приватной комнаты необходимо указать пароль");
                    return false;
                }

                if (PasswordBox.Password.Length < 8)
                {
                    ErrorMessageVisible("*Пароль должен содержать минимум 8 символов");
                    return false;
                }
            }

            return true;
        }

        // Методы для работы с сообщением об ошибке
        private void ErrorMessageVisible(string message)
        {
            if (ErrorMessage != null)
            {
                ErrorMessage.Visibility = Visibility.Visible;
                ErrorMessage.Text = message;
            }
        }

        private void ErrorMessageHide()
        {
            if (ErrorMessage != null)
            {
                ErrorMessage.Visibility = Visibility.Collapsed;
            }
        }
    }
}