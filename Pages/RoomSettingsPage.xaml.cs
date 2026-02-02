using System;
using System.Windows;
using System.Windows.Controls;
using TrackSync.DataBase;
//using TrackSync.DataBase.Model;
using TrackSync.DataBase.WorkWithDB;
using TrackSync.Windows;

namespace TrackSync.Pages
{
    public partial class RoomSettingsPage : Page
    {
        private int _roomId;
        private HikeRoom _currentRoom;

        public RoomSettingsPage(int roomId)
        {
            InitializeComponent();
            _roomId = roomId;
            LoadRoomData();
            LoadStatuses();
        }

        // Загрузка данных комнаты
        private void LoadRoomData()
        {
            try
            {
                using var ctx = new TrackSyncContext();
                _currentRoom = ctx.HikeRooms.Find(_roomId);

                if (_currentRoom != null)
                {
                    // Заполняем поля данными из БД
                    TitleTextBox.Text = _currentRoom.Title;
                    DescriptionTextBox.Text = _currentRoom.Description ?? string.Empty;

                    // Устанавливаем тип комнаты
                    if (_currentRoom.AccessType)
                    {
                        PublicRoomRadio.IsChecked = true;
                        PasswordPanel.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        PrivateRoomRadio.IsChecked = true;
                        PasswordPanel.Visibility = Visibility.Visible;
                    }

                    // Устанавливаем даты
                    //StartDatePicker.SelectedDate = _currentRoom.StartDate;
                    //EndDatePicker.SelectedDate = _currentRoom.EndDate;

                    // Устанавливаем статус (будет загружен в LoadStatuses)
                }
                else
                {
                    ErrorMessageVisible("*Не удалось загрузить данные комнаты");
                }
            }
            catch (Exception ex)
            {
                ErrorMessageVisible($"*Ошибка при загрузке данных: {ex.Message}");
            }
        }

        // Загрузка статусов из БД
        private void LoadStatuses()
        {
            try
            {
                using var ctx = new TrackSyncContext();

                // Очищаем комбобокс
                StatusComboBox.Items.Clear();

                // Загружаем статусы из БД
                var statuses = ctx.Statuses;
                foreach (var status in statuses)
                {
                    StatusComboBox.Items.Add(new ComboBoxItem
                    {
                        Content = status.Title,
                        Tag = status.StatusId
                    });

                    // Устанавливаем текущий статус комнаты
                    if (_currentRoom != null && status.StatusId == _currentRoom.StatusId)
                    {
                        StatusComboBox.SelectedIndex = StatusComboBox.Items.Count - 1;
                    }
                }

                // Если статус не установлен, выбираем первый
                if (StatusComboBox.SelectedIndex == -1 && StatusComboBox.Items.Count > 0)
                {
                    StatusComboBox.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                ErrorMessageVisible($"*Ошибка при загрузке статусов: {ex.Message}");
            }
        }

        // Обработчик изменения типа комнаты
        private void RoomTypeRadio_Checked(object sender, RoutedEventArgs e)
        {
            PasswordPanel.Visibility = PrivateRoomRadio.IsChecked == true
                ? Visibility.Visible
                : Visibility.Collapsed;
            ErrorMessageHide();
        }

        // Обработчики для скрытия ошибок при вводе
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

            if (StartDatePicker.SelectedDate.HasValue && EndDatePicker.SelectedDate.HasValue)
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

            if (StartDatePicker.SelectedDate.HasValue && EndDatePicker.SelectedDate.HasValue)
            {
                if (EndDatePicker.SelectedDate.Value < StartDatePicker.SelectedDate.Value)
                {
                    ErrorMessageVisible("*Дата окончания не может быть раньше даты начала!");
                }
            }
        }

        // Сохранение изменений
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorMessageHide();

            // Валидация полей
            if (!ValidateForm())
                return;

            try
            {
                using var ctx = new TrackSyncContext();
                var room = ctx.HikeRooms.Find(_roomId);

                if (room == null)
                {
                    ErrorMessageVisible("*Комната не найдена");
                    return;
                }

                //// Обновляем данные комнаты
                //room.Title = TitleTextBox.Text.Trim();
                //room.Description = string.IsNullOrWhiteSpace(DescriptionTextBox.Text)
                //    ? null
                //    : DescriptionTextBox.Text;
                //room.AccessType = PublicRoomRadio.IsChecked.Value;
                //room.StartDate = StartDatePicker.SelectedDate.Value;
                //room.EndDate = EndDatePicker.SelectedDate.Value;

                //// Обновляем статус
                //if (StatusComboBox.SelectedItem is ComboBoxItem selectedItem &&
                //    selectedItem.Tag is int statusId)
                //{
                //    room.StatusId = statusId;
                //}

                // Обновляем пароль, если комната приватная
                if (!room.AccessType && !string.IsNullOrWhiteSpace(PasswordBox.Password))
                {
                    if (PasswordBox.Password.Length < 8)
                    {
                        ErrorMessageVisible("*Пароль должен содержать минимум 8 символов");
                        return;
                    }
                    room.Password = BCrypt.Net.BCrypt.HashPassword(PasswordBox.Password);
                }

                // Сохраняем изменения в БД
                ctx.SaveChanges();

                // Обновляем права участников (заглушка - в реальном приложении нужно обновлять права)
                UpdateParticipantsPermissions();

                MessageBox.Show("Настройки комнаты успешно сохранены!",
                              "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                // Возвращаемся на страницу комнаты
                FrameContext.MainWindowFrame.Navigate(new HikeRoomPage(_roomId));
            }
            catch (Exception ex)
            {
                ErrorMessageVisible($"*Ошибка при сохранении: {ex.Message}");
            }
        }

        // Обновление прав участников (заглушка)
        private void UpdateParticipantsPermissions()
        {
            // В реальном приложении здесь нужно обновить права участников в БД
            // Для примера просто выводим сообщение
            // Пример: 
            // var participant = ctx.Participants.Find(participantId);
            // participant.EditMealPlan = EditMealPlanCheck1.IsChecked.Value;
            // participant.EditTrack = EditTrackCheck1.IsChecked.Value;
            // participant.EditEquipment = EditEquipmentCheck1.IsChecked.Value;
        }

        // Отмена изменений
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            FrameContext.MainWindowFrame.Navigate(new HikeRoomPage(_roomId));
        }

        // Валидация формы
        private bool ValidateForm()
        {
            // Проверка названия
            if (string.IsNullOrWhiteSpace(TitleTextBox.Text))
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
            if (!StartDatePicker.SelectedDate.HasValue)
            {
                ErrorMessageVisible("*Пожалуйста, выберите дату начала похода");
                return false;
            }

            if (!EndDatePicker.SelectedDate.HasValue)
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
            if (PrivateRoomRadio.IsChecked.Value)
            {
                if (string.IsNullOrWhiteSpace(PasswordBox.Password))
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
            ErrorMessage.Visibility = Visibility.Visible;
            ErrorMessage.Text = message;
        }

        private void ErrorMessageHide()
        {
            ErrorMessage.Visibility = Visibility.Collapsed;
        }
    }
}