using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using TrackSync.DataBase;
using TrackSync.Windows;

namespace TrackSync.Pages
{
    public partial class RoomSettingsPage : Page
    {
        private readonly int _roomId;
        private HikeRoom _currentRoom;
        private readonly int _currentUserId;
        private bool _isLeader;

        // Вспомогательный класс для отображения участника с правами
        private class ParticipantViewModel : INotifyPropertyChanged
        {
            private bool _editMealPlan;
            private bool _editTrack;
            private bool _editEquipment;
            private bool _isEditable;
            private bool _canExclude;

            public int ParticipantId { get; set; }
            public int UserId { get; set; }
            public string UserName { get; set; }
            public string Email { get; set; }
            public string Role { get; set; }
            public bool IsLeader { get; set; }

            public bool EditMealPlan
            {
                get => _editMealPlan;
                set
                {
                    if (_editMealPlan != value)
                    {
                        _editMealPlan = value;
                        OnPropertyChanged(nameof(EditMealPlan));
                    }
                }
            }

            public bool EditTrack
            {
                get => _editTrack;
                set
                {
                    if (_editTrack != value)
                    {
                        _editTrack = value;
                        OnPropertyChanged(nameof(EditTrack));
                    }
                }
            }

            public bool EditEquipment
            {
                get => _editEquipment;
                set
                {
                    if (_editEquipment != value)
                    {
                        _editEquipment = value;
                        OnPropertyChanged(nameof(EditEquipment));
                    }
                }
            }

            public bool IsEditable
            {
                get => _isEditable;
                set
                {
                    if (_isEditable != value)
                    {
                        _isEditable = value;
                        OnPropertyChanged(nameof(IsEditable));
                    }
                }
            }

            public bool CanExclude
            {
                get => _canExclude;
                set
                {
                    if (_canExclude != value)
                    {
                        _canExclude = value;
                        OnPropertyChanged(nameof(CanExclude));
                    }
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;
            protected void OnPropertyChanged(string propertyName) =>
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public RoomSettingsPage(int roomId)
        {
            InitializeComponent();
            _roomId = roomId;
            _currentUserId = SessionManager.CurrentUserId;

            LoadRoomData();
            LoadStatuses();
            LoadParticipants();
            ApplyEditPermissions();
        }

        // Загрузка данных комнаты
        private void LoadRoomData()
        {
            try
            {
                using var ctx = new TrackSyncContext();
                _currentRoom = ctx.HikeRooms
                    .Include(r => r.Leader) // Загружаем лидера для проверки прав
                    .FirstOrDefault(r => r.HikeRoomId == _roomId);

                if (_currentRoom == null)
                {
                    ErrorMessageVisible("*Комната не найдена");
                    CancelButton_Click(null, null);
                    return;
                }

                // Проверяем лидерство через поле LeaderId
                _isLeader = _currentRoom.LeaderId == _currentUserId;

                // Заполняем поля формы
                TitleTextBox.Text = _currentRoom.Title;
                DescriptionTextBox.Text = _currentRoom.Description ?? string.Empty;
                StartDatePicker.SelectedDate = _currentRoom.StartDate.ToDateTime(TimeOnly.MinValue);
                EndDatePicker.SelectedDate = _currentRoom.EndDate.ToDateTime(TimeOnly.MinValue);

                if (_currentRoom.AccessType)
                {
                    PublicRoomRadio.IsChecked = true;
                }
                else
                {
                    PrivateRoomRadio.IsChecked = true;
                }

                // Обновляем видимость панели пароля
                PasswordPanel.Visibility = !_currentRoom.AccessType ? Visibility.Visible : Visibility.Collapsed;
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
                StatusComboBox.Items.Clear();

                var statuses = ctx.Statuses.ToList();
                foreach (var status in statuses)
                {
                    var item = new ComboBoxItem
                    {
                        Content = status.Title,
                        Tag = status.StatusId
                    };
                    StatusComboBox.Items.Add(item);

                    if (_currentRoom != null && status.StatusId == _currentRoom.StatusId)
                    {
                        StatusComboBox.SelectedItem = item;
                    }
                }

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

        // Загрузка участников с правами
        private void LoadParticipants()
        {
            try
            {
                using var ctx = new TrackSyncContext();
                var participants = ctx.Participants
                    .Include(p => p.User)
                    .Where(p => p.HikeRoomId == _roomId)
                    .ToList();

                if (!participants.Any())
                {
                    NoParticipantsMessage.Visibility = Visibility.Visible;
                    ParticipantsItemsControl.Visibility = Visibility.Collapsed;
                    return;
                }

                NoParticipantsMessage.Visibility = Visibility.Collapsed;
                ParticipantsItemsControl.Visibility = Visibility.Visible;

                var participantViewModels = new List<ParticipantViewModel>();

                foreach (var participant in participants)
                {
                    bool isLeader = _currentRoom.LeaderId == participant.UserId;

                    participantViewModels.Add(new ParticipantViewModel
                    {
                        ParticipantId = participant.ParticipantId,
                        UserId = participant.UserId,
                        UserName = participant.User.Name,
                        Email = participant.User.Email,
                        Role = isLeader ? "Лидер" : "Участник",
                        IsLeader = isLeader,
                        EditMealPlan = participant.EditMealPlan ?? false,
                        EditTrack = participant.EditTrack ?? false,
                        EditEquipment = participant.EditEquipment ?? false,
                        IsEditable = _isLeader && !isLeader,
                        CanExclude = _isLeader && !isLeader // Только лидер может исключать, и нельзя исключить себя (лидера)
                    });
                }

                ParticipantsItemsControl.ItemsSource = participantViewModels;
            }
            catch (Exception ex)
            {
                ErrorMessageVisible($"*Ошибка при загрузке участников: {ex.Message}");
            }
        }

        // Применение прав редактирования в зависимости от роли
        private void ApplyEditPermissions()
        {
            bool isEditable = _isLeader;

            // Поля комнаты
            TitleTextBox.IsReadOnly = !isEditable;
            DescriptionTextBox.IsReadOnly = !isEditable;
            PublicRoomRadio.IsEnabled = isEditable;
            PrivateRoomRadio.IsEnabled = isEditable;
            StartDatePicker.IsEnabled = isEditable;
            EndDatePicker.IsEnabled = isEditable;
            StatusComboBox.IsEnabled = isEditable;

            // Пароль: видим только для приватной комнаты
            bool isPrivateRoom = PrivateRoomRadio.IsChecked == true;
            PasswordPanel.Visibility = isPrivateRoom ? Visibility.Visible : Visibility.Collapsed;
            PasswordBox.IsEnabled = isEditable && isPrivateRoom;

            // Кнопки
            SaveButton.Visibility = isEditable ? Visibility.Visible : Visibility.Collapsed;
            CancelButton.Content = isEditable ? "Отмена" : "Назад";
        }

        // Обработчик кнопки исключения участника
        private void ExcludeButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int participantId)
            {
                ExcludeParticipant(participantId);
            }
        }

        // Исключение участника из комнаты
        private void ExcludeParticipant(int participantId)
        {
            // Подтверждение действия
            var result = MessageBox.Show("Вы уверены, что хотите исключить этого участника из комнаты?",
                "Подтверждение исключения",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                using var ctx = new TrackSyncContext();

                // Находим участника
                var participant = ctx.Participants
                    .Include(p => p.User)
                    .FirstOrDefault(p => p.ParticipantId == participantId && p.HikeRoomId == _roomId);

                if (participant == null)
                {
                    MessageBox.Show("Участник не найден", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Проверяем, что текущий пользователь - лидер
                if (!_isLeader)
                {
                    MessageBox.Show("Только лидер комнаты может исключать участников", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Проверяем, что не пытаемся исключить лидера
                if (participant.UserId == _currentRoom.LeaderId)
                {
                    MessageBox.Show("Нельзя исключить лидера комнаты", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Удаляем связанные данные участника в правильном порядке

                // 1. Удаляем снаряжение участника
                var equipment = ctx.HikeRoomsEquipments
                    .Where(hre => hre.ParticipantId == participantId)
                    .ToList();
                ctx.HikeRoomsEquipments.RemoveRange(equipment);

                // 2. Удаляем связи участника с рецептами
                var recipeParticipants = ctx.RecipesParticipants
                    .Where(rp => rp.ParticipantId == participantId)
                    .ToList();
                ctx.RecipesParticipants.RemoveRange(recipeParticipants);

                // 3. Удаляем треки участника и связанные с ними данные
                var tracks = ctx.Tracks
                    .Include(t => t.Segments)
                    .Where(t => t.ParticipantId == participantId)
                    .ToList();

                foreach (var track in tracks)
                {
                    // Удаляем сегменты трека
                    var segments = track.Segments.ToList();
                    ctx.Segments.RemoveRange(segments);

                    // Удаляем трек
                    ctx.Tracks.Remove(track);
                }

                // 4. Удаляем заявки участника на вступление в эту комнату (если есть)
                var bids = ctx.Bids
                    .Where(b => b.UserId == participant.UserId && b.HikeRoomId == _roomId)
                    .ToList();
                ctx.Bids.RemoveRange(bids);

                // 5. Удаляем приглашения участника (если есть)
                var invitations = ctx.Invitations
                    .Where(i => i.UserId == participant.UserId && i.HikeRoomId == _roomId)
                    .ToList();
                ctx.Invitations.RemoveRange(invitations);

                // 6. Удаляем самого участника
                ctx.Participants.Remove(participant);

                ctx.SaveChanges();

                // Обновляем список участников
                LoadParticipants();

                MessageBox.Show($"Участник {participant.User.Name} успешно исключен из комнаты",
                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при исключении участника:\n{ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Обработчик изменения типа комнаты
        private void RoomTypeRadio_Checked(object sender, RoutedEventArgs e)
        {
            bool isPrivate = PrivateRoomRadio.IsChecked == true;
            PasswordPanel.Visibility = isPrivate ? Visibility.Visible : Visibility.Collapsed;

            // Очищаем пароль при переключении на публичную комнату
            if (!isPrivate)
            {
                PasswordBox.Password = string.Empty;
            }

            ErrorMessageHide();
        }

        // Валидация и сохранение
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorMessageHide();

            if (!ValidateForm())
                return;

            try
            {
                using var ctx = new TrackSyncContext();
                var room = ctx.HikeRooms
                    .Include(r => r.Participants)
                    .ThenInclude(p => p.User)
                    .FirstOrDefault(r => r.HikeRoomId == _roomId);

                if (room == null)
                {
                    ErrorMessageVisible("*Комната не найдена");
                    return;
                }

                // Обновляем данные комнаты
                room.Title = TitleTextBox.Text.Trim();
                room.Description = string.IsNullOrWhiteSpace(DescriptionTextBox.Text)
                    ? null
                    : DescriptionTextBox.Text.Trim();
                room.AccessType = PublicRoomRadio.IsChecked.Value;

                if (StartDatePicker.SelectedDate.HasValue)
                    room.StartDate = DateOnly.FromDateTime(StartDatePicker.SelectedDate.Value.Date);
                if (EndDatePicker.SelectedDate.HasValue)
                    room.EndDate = DateOnly.FromDateTime(EndDatePicker.SelectedDate.Value.Date);

                // Обновляем статус
                if (StatusComboBox.SelectedItem is ComboBoxItem selectedItem &&
                    selectedItem.Tag is int statusId)
                {
                    room.StatusId = statusId;
                }

                // Обновляем пароль ТОЛЬКО если он был изменён и комната приватная
                if (!room.AccessType)
                {
                    if (!string.IsNullOrWhiteSpace(PasswordBox.Password))
                    {
                        if (PasswordBox.Password.Length < 8)
                        {
                            ErrorMessageVisible("*Пароль должен содержать минимум 8 символов");
                            return;
                        }
                        room.Password = BCrypt.Net.BCrypt.EnhancedHashPassword(PasswordBox.Password);
                    }
                }
                else
                {
                    // Для публичной комнаты очищаем пароль
                    room.Password = null;
                }

                // Сохраняем изменения комнаты
                ctx.SaveChanges();

                // Обновляем права участников (только если лидер)
                if (_isLeader && ParticipantsItemsControl.ItemsSource is List<ParticipantViewModel> participants)
                {
                    UpdateParticipantsPermissions(ctx, participants);
                }

                FrameContext.MainWindowFrame.Navigate(new HikeRoomPage(_roomId));
            }
            catch (Exception ex)
            {
                ErrorMessageVisible($"*Ошибка при сохранении: {ex.Message}");
            }
        }

        // Обновление прав участников в БД
        private void UpdateParticipantsPermissions(TrackSyncContext ctx, List<ParticipantViewModel> participants)
        {
            foreach (var vm in participants)
            {
                // Пропускаем лидера — его права не должны изменяться
                if (vm.IsLeader)
                    continue;

                var participant = ctx.Participants.Find(vm.ParticipantId);
                if (participant != null)
                {
                    participant.EditMealPlan = vm.EditMealPlan;
                    participant.EditTrack = vm.EditTrack;
                    participant.EditEquipment = vm.EditEquipment;
                }
            }

            ctx.SaveChanges();
        }

        // Отмена/назад
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            FrameContext.MainWindowFrame.Navigate(new HikeRoomPage(_roomId));
        }

        // Валидация формы
        private bool ValidateForm()
        {
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

            if (StartDatePicker.SelectedDate.Value.Date < DateTime.Today.Date)
            {
                ErrorMessageVisible("*Дата начала не может быть в прошлом!");
                return false;
            }

            if (EndDatePicker.SelectedDate.Value.Date < StartDatePicker.SelectedDate.Value.Date)
            {
                ErrorMessageVisible("*Дата окончания не может быть раньше даты начала!");
                return false;
            }

            if (PrivateRoomRadio.IsChecked == true)
            {
                // Проверяем пароль только если поле не пустое (новый пароль)
                if (!string.IsNullOrWhiteSpace(PasswordBox.Password) && PasswordBox.Password.Length < 8)
                {
                    ErrorMessageVisible("*Пароль должен содержать минимум 8 символов");
                    return false;
                }
            }

            return true;
        }

        // Вспомогательные методы для ошибок
        private void ErrorMessageVisible(string message)
        {
            ErrorMessage.Visibility = Visibility.Visible;
            ErrorMessage.Text = message;
        }

        private void ErrorMessageHide()
        {
            ErrorMessage.Visibility = Visibility.Collapsed;
        }

        // Обработчики для скрытия ошибок при вводе
        private void TitleTextBox_TextChanged(object sender, TextChangedEventArgs e) => ErrorMessageHide();
        private void DescriptionTextBox_TextChanged(object sender, TextChangedEventArgs e) => ErrorMessageHide();
        private void StartDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e) => ErrorMessageHide();
        private void EndDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e) => ErrorMessageHide();
    }
}