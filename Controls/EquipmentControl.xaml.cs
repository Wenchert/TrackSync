using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TrackSync.DataBase;
using TrackSync.DataBase.Model;
using TrackSync.DataBase.WorkWithDB;
using TrackSync.Interfaces;
using TrackSync.Pages;

namespace TrackSync.Controls
{
    //public partial class EquipmentControl : UserControl, IEquipmentCallback
    //{
    //    private int _roomId;
    //    private int _currentParticipantId = 1;
    //    private readonly Dictionary<int, List<EquipmentQuantityItem>> _participantEquipment = new();
    //    private bool _canEditEquipment = false;

    //    // Вспомогательный класс для хранения снаряжения с количеством
    //    private class EquipmentQuantityItem
    //    {
    //        public Equipment Equipment { get; set; }
    //        public int Quantity { get; set; }
    //    }

    //    // Событие для оповещения родительской страницы
    //    public event EventHandler EquipmentSaved;
    //    public event EventHandler<int> AddEquipmentRequested;

    //    // Dependency Properties для привязки данных
    //    public static readonly DependencyProperty RoomIdProperty =
    //        DependencyProperty.Register("RoomId", typeof(int), typeof(EquipmentControl),
    //            new PropertyMetadata(0, OnRoomIdChanged));

    //    public static readonly DependencyProperty CanEditEquipmentProperty =
    //        DependencyProperty.Register("CanEditEquipment", typeof(bool), typeof(EquipmentControl),
    //            new PropertyMetadata(false, OnCanEditEquipmentChanged));

    //    public int RoomId
    //    {
    //        get => (int)GetValue(RoomIdProperty);
    //        set => SetValue(RoomIdProperty, value);
    //    }

    //    public bool CanEditEquipment
    //    {
    //        get => (bool)GetValue(CanEditEquipmentProperty);
    //        set => SetValue(CanEditEquipmentProperty, value);
    //    }

    //    public EquipmentControl()
    //    {
    //        InitializeComponent();
    //        Loaded += EquipmentControl_Loaded;
    //    }

    //    private void EquipmentControl_Loaded(object sender, RoutedEventArgs e)
    //    {
    //        // Если RoomId уже установлен, загружаем данные
    //        if (RoomId > 0)
    //        {
    //            LoadParticipants();
    //        }
    //    }

    //    private static void OnRoomIdChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    //    {
    //        var control = (EquipmentControl)d;
    //        if (control.IsLoaded && (int)e.NewValue > 0)
    //        {
    //            control.LoadParticipants();
    //        }
    //    }

    //    private static void OnCanEditEquipmentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    //    {
    //        var control = (EquipmentControl)d;
    //        if (control.IsLoaded)
    //        {
    //            control.ApplyPermissions();
    //        }
    //    }

    //    // Загрузка участников комнаты со снаряжением
    //    private void LoadParticipants()
    //    {
    //        try
    //        {
    //            var participants = TrackSyncManagerList.GetParticipantsWithEquipment(_roomId);

    //            if (participants != null && participants.Any())
    //            {
    //                // Очищаем словарь перед загрузкой новых данных
    //                _participantEquipment.Clear();

    //                // Загружаем снаряжение для каждого участника из БД
    //                foreach (var participant in participants)
    //                {
    //                    var equipmentList = new List<EquipmentQuantityItem>();
    //                    foreach (var hre in participant.HikeRoomsEquipments)
    //                    {
    //                        if (hre.Equipment != null)
    //                        {
    //                            equipmentList.Add(new EquipmentQuantityItem
    //                            {
    //                                Equipment = hre.Equipment,
    //                                Quantity = hre.Quantity
    //                            });
    //                        }
    //                    }

    //                    // Сохраняем список снаряжения для участника в словарь
    //                    _participantEquipment[participant.ParticipantId] = equipmentList;
    //                }

    //                // Устанавливаем источник данных для отображения участников
    //                ParticipantsItemsControl.ItemsSource = participants;

    //                // Подписываемся на событие загрузки контейнера для отложенного обновления интерфейса
    //                ParticipantsItemsControl.Loaded += (s, e) =>
    //                {
    //                    Dispatcher.BeginInvoke(new Action(() =>
    //                    {
    //                        // Обновляем интерфейс снаряжения для КАЖДОГО участника
    //                        foreach (var participant in participants)
    //                        {
    //                            UpdateEquipmentUI(participant.ParticipantId);
    //                        }
    //                        ApplyPermissions();
    //                    }), System.Windows.Threading.DispatcherPriority.Background);
    //                };
    //            }
    //        }
    //        catch (Exception ex)
    //        {
    //            MessageBox.Show($"Ошибка загрузки участников: {ex.Message}", "Ошибка",
    //                MessageBoxButton.OK, MessageBoxImage.Error);
    //        }
    //    }

    //    // Обновление интерфейса снаряжения для участника
    //    private void UpdateEquipmentUI(int participantId)
    //    {
    //        var equipmentStackPanel = FindEquipmentStackPanel(participantId);
    //        if (equipmentStackPanel == null)
    //            return;

    //        equipmentStackPanel.Children.Clear();

    //        if (_participantEquipment.TryGetValue(participantId, out var equipmentList) && equipmentList.Any())
    //        {
    //            foreach (var item in equipmentList)
    //            {
    //                // Передаём participantId в метод создания панели
    //                var equipmentPanel = CreateEquipmentPanel(item, participantId);
    //                equipmentStackPanel.Children.Add(equipmentPanel);
    //            }

    //            UpdateEquipmentStats(participantId, equipmentList);
    //        }
    //        else
    //        {
    //            var emptyText = new TextBlock
    //            {
    //                Style = (Style)FindResource("BodyText"),
    //                Foreground = (Brush)new BrushConverter().ConvertFrom("#7F8C8D"),
    //                Text = "Нет добавленного снаряжения",
    //                Margin = new Thickness(0, 5, 0, 0)
    //            };
    //            equipmentStackPanel.Children.Add(emptyText);
    //            UpdateEquipmentStats(participantId, new List<EquipmentQuantityItem>());
    //        }
    //    }

    //    // Создание панели для отображения снаряжения
    //    private StackPanel CreateEquipmentPanel(EquipmentQuantityItem item, int participantId)
    //    {
    //        var panel = new StackPanel
    //        {
    //            Orientation = Orientation.Horizontal,
    //            Margin = new Thickness(0, 0, 0, 8),
    //            Tag = item
    //        };

    //        // Название снаряжения
    //        var nameText = new TextBlock
    //        {
    //            Text = item.Equipment.Title,
    //            Style = (Style)FindResource("BodyText"),
    //            VerticalAlignment = VerticalAlignment.Center,
    //            Width = 220,
    //            TextWrapping = TextWrapping.Wrap
    //        };
    //        panel.Children.Add(nameText);

    //        // Поле для ввода количества
    //        var quantityTextBox = new TextBox
    //        {
    //            Width = 80,
    //            Height = 36,
    //            Margin = new Thickness(10, 0, 0, 0),
    //            Text = item.Quantity.ToString(),
    //            Tag = item,
    //            Style = (Style)FindResource("InputField")
    //        };

    //        if (CanEditEquipment)
    //        {
    //            quantityTextBox.TextChanged += QuantityTextBox_TextChanged;
    //            quantityTextBox.PreviewTextInput += QuantityTextBox_PreviewTextInput;
    //        }
    //        else
    //        {
    //            quantityTextBox.IsReadOnly = true;
    //            quantityTextBox.Background = Brushes.LightGray;
    //        }

    //        panel.Children.Add(quantityTextBox);

    //        // Текст "шт."
    //        var unitText = new TextBlock
    //        {
    //            Text = "шт.",
    //            Style = (Style)FindResource("BodyText"),
    //            VerticalAlignment = VerticalAlignment.Center,
    //            Margin = new Thickness(5, 0, 0, 0),
    //            Foreground = (Brush)new BrushConverter().ConvertFrom("#7F8C8D")
    //        };
    //        panel.Children.Add(unitText);

    //        // Вес одного предмета
    //        var weightText = new TextBlock
    //        {
    //            Text = $"({item.Equipment.Weight}г)",
    //            Style = (Style)FindResource("BodyText"),
    //            VerticalAlignment = VerticalAlignment.Center,
    //            Margin = new Thickness(10, 0, 0, 0),
    //            Foreground = (Brush)new BrushConverter().ConvertFrom("#7F8C8D"),
    //            FontSize = 12
    //        };
    //        panel.Children.Add(weightText);

    //        // Кнопка удаления (только если есть права)
    //        if (CanEditEquipment)
    //        {
    //            var removeButton = new Button
    //            {
    //                Width = 36,
    //                Height = 36,
    //                Style = (Style)FindResource("IconButton"),
    //                Margin = new Thickness(10, 0, 0, 0),
    //                Tag = new { Item = item, ParticipantId = participantId },
    //                ToolTip = "Удалить снаряжение"
    //            };
    //            var image = new Image
    //            {
    //                Source = new BitmapImage(new Uri("/Images/Icons/trash.png", UriKind.Relative)),
    //                Width = 18,
    //                Height = 18
    //            };
    //            removeButton.Content = image;
    //            removeButton.Click += RemoveEquipmentButton_Click;
    //            panel.Children.Add(removeButton);
    //        }

    //        return panel;
    //    }

    //    // Обработчик изменения количества
    //    private void QuantityTextBox_TextChanged(object sender, TextChangedEventArgs e)
    //    {
    //        if (sender is TextBox textBox && textBox.Tag is EquipmentQuantityItem item)
    //        {
    //            // Сохраняем текущее значение для восстановления в случае ошибки
    //            int previousQuantity = item.Quantity;

    //            // Обработка пустой строки — интерпретируем как 0
    //            if (string.IsNullOrWhiteSpace(textBox.Text))
    //            {
    //                item.Quantity = 0;

    //                // Автоматически обновляем статистику
    //                if (_participantEquipment.TryGetValue(_currentParticipantId, out var equipmentList))
    //                {
    //                    UpdateEquipmentStats(_currentParticipantId, equipmentList);
    //                }
    //                return;
    //            }

    //            // Попытка преобразовать в число
    //            if (int.TryParse(textBox.Text, out int quantity))
    //            {
    //                // Разрешаем 0 и положительные числа
    //                if (quantity >= 0)
    //                {
    //                    item.Quantity = quantity;

    //                    // Автоматически обновляем статистику
    //                    if (_participantEquipment.TryGetValue(_currentParticipantId, out var equipmentList))
    //                    {
    //                        UpdateEquipmentStats(_currentParticipantId, equipmentList);
    //                    }
    //                }
    //                else
    //                {
    //                    // Восстанавливаем предыдущее значение при отрицательном числе
    //                    textBox.Text = previousQuantity.ToString();
    //                    textBox.SelectionStart = textBox.Text.Length;
    //                }
    //            }
    //            else
    //            {
    //                // Восстанавливаем предыдущее значение при некорректном вводе
    //                textBox.Text = previousQuantity.ToString();
    //                textBox.SelectionStart = textBox.Text.Length;
    //            }
    //        }
    //    }

    //    // Валидация ввода (только цифры)
    //    private void QuantityTextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
    //    {
    //        foreach (char c in e.Text)
    //        {
    //            if (!char.IsDigit(c))
    //            {
    //                e.Handled = true;
    //                break;
    //            }
    //        }
    //    }

    //    // Удаление снаряжения
    //    private void RemoveEquipmentButton_Click(object sender, RoutedEventArgs e)
    //    {
    //        // Получаем данные из тега кнопки
    //        if (sender is Button button &&
    //            button.Tag is object tagObj &&
    //            tagObj.GetType().GetProperty("Item")?.GetValue(tagObj) is EquipmentQuantityItem item &&
    //            tagObj.GetType().GetProperty("ParticipantId")?.GetValue(tagObj) is int participantId)
    //        {
    //            // Получаем список снаряжения именно для этого участника
    //            if (_participantEquipment.TryGetValue(participantId, out var equipmentList))
    //            {
    //                equipmentList.Remove(item);
    //                UpdateEquipmentUI(participantId); // Обновляем интерфейс для этого участника
    //            }
    //        }
    //    }

    //    // Обновление статистики снаряжения
    //    private void UpdateEquipmentStats(int participantId, List<EquipmentQuantityItem> equipmentList)
    //    {
    //        // Вычисляем общий вес и количество предметов для данного участника
    //        int totalWeight = 0;
    //        int totalItems = 0;

    //        foreach (var item in equipmentList)
    //        {
    //            totalWeight += item.Equipment.Weight * item.Quantity;
    //            totalItems += item.Quantity;
    //        }

    //        // Ищем элементы статистики ТОЛЬКО для данного участника
    //        var container = ParticipantsItemsControl.ItemContainerGenerator.ContainerFromItem(
    //            ParticipantsItemsControl.Items.OfType<Participant>().FirstOrDefault(p => p.ParticipantId == participantId));

    //        if (container != null)
    //        {
    //            var weightText = FindVisualChild<TextBlock>(container, tb => tb.Name == "TotalWeightText");
    //            var itemsText = FindVisualChild<TextBlock>(container, tb => tb.Name == "TotalItemsText");

    //            if (weightText != null)
    //                weightText.Text = $"{totalWeight / 1000.0:F1} кг";

    //            if (itemsText != null)
    //                itemsText.Text = $"{totalItems} шт.";
    //        }
    //    }

    //    // Поиск стек панели для снаряжения по ID участника (ищем по тегу)
    //    private StackPanel FindEquipmentStackPanel(int participantId)
    //    {
    //        foreach (var item in ParticipantsItemsControl.Items)
    //        {
    //            var container = ParticipantsItemsControl.ItemContainerGenerator.ContainerFromItem(item);
    //            if (container == null) continue;

    //            // Ищем ТОЛЬКО панели с тегом = participantId
    //            var panels = FindAllVisualChildren<StackPanel>(container);
    //            foreach (var panel in panels)
    //            {
    //                if (panel.Tag?.ToString() == participantId.ToString())
    //                {
    //                    return panel;
    //                }
    //            }
    //        }
    //        return null;
    //    }

    //    // Вспомогательный метод для поиска ВСЕХ дочерних элементов заданного типа
    //    private List<T> FindAllVisualChildren<T>(DependencyObject parent) where T : DependencyObject
    //    {
    //        var children = new List<T>();
    //        if (parent == null) return children;

    //        int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
    //        for (int i = 0; i < childrenCount; i++)
    //        {
    //            var child = VisualTreeHelper.GetChild(parent, i);

    //            if (child is T typedChild)
    //                children.Add(typedChild);

    //            children.AddRange(FindAllVisualChildren<T>(child));
    //        }

    //        return children;
    //    }

    //    // Вспомогательный метод для поиска дочернего элемента с предикатом
    //    private T FindVisualChild<T>(DependencyObject parent, Func<T, bool> predicate) where T : DependencyObject
    //    {
    //        if (parent == null) return null;

    //        int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
    //        for (int i = 0; i < childrenCount; i++)
    //        {
    //            var child = VisualTreeHelper.GetChild(parent, i);

    //            if (child is T typedChild && predicate(typedChild))
    //                return typedChild;

    //            var result = FindVisualChild<T>(child, predicate);
    //            if (result != null)
    //                return result;
    //        }

    //        return null;
    //    }

    //    // Вспомогательный метод для поиска дочернего элемента 
    //    private T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    //    {
    //        if (parent == null) return null;

    //        int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
    //        for (int i = 0; i < childrenCount; i++)
    //        {
    //            var child = VisualTreeHelper.GetChild(parent, i);

    //            if (child is T typedChild)
    //                return typedChild;

    //            var result = FindVisualChild<T>(child);
    //            if (result != null)
    //                return result;
    //        }

    //        return null;
    //    }

    //    // Обработчик кнопки "Добавить снаряжение"
    //    private void AddEquipmentButton_Click(object sender, RoutedEventArgs e)
    //    {
    //        if (sender is Button button && button.Tag is int participantId)
    //        {
    //            _currentParticipantId = participantId;
    //            AddEquipmentRequested?.Invoke(this, participantId);
    //        }
    //    }

    //    // Сохранение изменений
    //    private void SaveChangesButton_Click(object sender, RoutedEventArgs e)
    //    {
    //        try
    //        {
    //            using (var ctx = new TrackSyncContext())
    //            {
    //                int addedCount = 0;
    //                int updatedCount = 0;
    //                int deletedCount = 0;

    //                // Получаем ВСЕ существующие записи снаряжения для этой комнаты из БД
    //                var existingRecords = ctx.HikeRoomsEquipments
    //                    .Where(hre => hre.HikeRoomId == _roomId)
    //                    .ToList();

    //                // Обрабатываем каждого участника
    //                foreach (var item in ParticipantsItemsControl.Items)
    //                {
    //                    if (item is Participant participant)
    //                    {
    //                        int participantId = participant.ParticipantId;

    //                        if (_participantEquipment.TryGetValue(participantId, out var equipmentList))
    //                        {
    //                            // 1. Добавляем/обновляем снаряжение из интерфейса
    //                            foreach (var eqItem in equipmentList)
    //                            {
    //                                if (eqItem.Quantity <= 0)
    //                                    continue; // Пропускаем записи с количеством 0 или меньше

    //                                // Ищем существующую запись для этого снаряжения у участника
    //                                var existingRecord = existingRecords
    //                                    .FirstOrDefault(hre =>
    //                                        hre.ParticipantId == participantId &&
    //                                        hre.EquipmentId == eqItem.Equipment.EquipmentId);

    //                                if (existingRecord != null)
    //                                {
    //                                    // Обновляем существующую запись
    //                                    existingRecord.Quantity = eqItem.Quantity;
    //                                    updatedCount++;
    //                                }
    //                                else
    //                                {
    //                                    // Создаём новую запись
    //                                    var newRecord = new HikeRoomsEquipment
    //                                    {
    //                                        HikeRoomId = _roomId,
    //                                        ParticipantId = participantId,
    //                                        EquipmentId = eqItem.Equipment.EquipmentId,
    //                                        Quantity = eqItem.Quantity
    //                                    };
    //                                    ctx.HikeRoomsEquipments.Add(newRecord);
    //                                    addedCount++;
    //                                }
    //                            }

    //                            // 2. Удаляем записи, которых НЕТ в интерфейсе (удалённые пользователем)
    //                            var recordsToDelete = existingRecords
    //                                .Where(hre => hre.ParticipantId == participantId &&
    //                                             !equipmentList.Any(eq =>
    //                                                 eq.Equipment.EquipmentId == hre.EquipmentId &&
    //                                                 eq.Quantity > 0)) // Учитываем только записи с количеством > 0
    //                                .ToList();

    //                            foreach (var record in recordsToDelete)
    //                            {
    //                                ctx.HikeRoomsEquipments.Remove(record);
    //                                deletedCount++;
    //                            }
    //                        }
    //                    }
    //                }

    //                ShowMessage("Изменения сохранены", true);
    //                ctx.SaveChanges();

    //                // Оповещаем родительскую страницу
    //                EquipmentSaved?.Invoke(this, EventArgs.Empty);
    //            }
    //        }
    //        catch (Exception ex)
    //        {
    //            MessageBox.Show($"Ошибка при сохранении снаряжения:\n{ex}", "Ошибка",
    //                MessageBoxButton.OK, MessageBoxImage.Error);
    //        }
    //    }

    //    // Обновление прав доступа
    //    private void ApplyPermissions()
    //    {
    //        SaveChangesButton.Visibility = CanEditEquipment ? Visibility.Visible : Visibility.Collapsed;

    //        // Обновляем все контейнеры участников
    //        foreach (var item in ParticipantsItemsControl.Items)
    //        {
    //            var container = ParticipantsItemsControl.ItemContainerGenerator.ContainerFromItem(item);
    //            if (container == null) continue;

    //            // Ищем кнопку "Добавить снаряжение" по ToolTip
    //            var addButton = FindVisualChild<Button>(container, btn =>
    //                btn.ToolTip != null && btn.ToolTip.ToString() == "Добавить снаряжение");

    //            if (addButton != null)
    //            {
    //                addButton.Visibility = CanEditEquipment ? Visibility.Visible : Visibility.Collapsed;
    //            }

    //            // Если нет прав, скрываем все кнопки удаления для этого участника
    //            if (!CanEditEquipment)
    //            {
    //                var removeButtons = FindAllVisualChildren<Button>(container)
    //                    .Where(btn => btn.ToolTip?.ToString() == "Удалить снаряжение");

    //                foreach (var btn in removeButtons)
    //                {
    //                    btn.Visibility = Visibility.Collapsed;
    //                }

    //                // Делаем поля количества только для чтения
    //                var quantityTextBoxes = FindAllVisualChildren<TextBox>(container)
    //                    .Where(tb => tb.Tag is EquipmentQuantityItem);

    //                foreach (var tb in quantityTextBoxes)
    //                {
    //                    tb.IsReadOnly = true;
    //                    tb.Background = Brushes.LightGray;
    //                }
    //            }
    //        }
    //    }

    //    // Показ сообщения
    //    private void ShowMessage(string message, bool isSuccess)
    //    {
    //        SaveSuccessMessage.Visibility = Visibility.Visible;
    //        SaveSuccessMessage.Text = message;
    //        SaveSuccessMessage.Foreground = isSuccess ? Brushes.Green : Brushes.Red;

    //        var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
    //        timer.Tick += (s, args) =>
    //        {
    //            SaveSuccessMessage.Visibility = Visibility.Collapsed;
    //            timer.Stop();
    //        };
    //        timer.Start();
    //    }

    //    // Реализация интерфейса IEquipmentCallback
    //    public void OnEquipmentSelected(List<DataBase.Equipment> equipment)
    //    {
    //        if (equipment == null || equipment.Count == 0)
    //            return;

    //        // Получаем или создаём список снаряжения для участника
    //        if (!_participantEquipment.TryGetValue(_currentParticipantId, out var participantList))
    //        {
    //            participantList = new List<EquipmentQuantityItem>();
    //            _participantEquipment[_currentParticipantId] = participantList;
    //        }

    //        // Добавляем новые снаряжения (без дубликатов)
    //        foreach (var eq in equipment)
    //        {
    //            if (!participantList.Exists(e => e.Equipment.EquipmentId == eq.EquipmentId))
    //            {
    //                participantList.Add(new EquipmentQuantityItem
    //                {
    //                    Equipment = eq,
    //                    Quantity = 1 // Значение по умолчанию
    //                });
    //            }
    //        }

    //        // Обновляем отображение для текущего участника
    //        UpdateEquipmentUI(_currentParticipantId);
    //    }

    //    // Метод для обновления данных извне
    //    public void RefreshData()
    //    {
    //        if (RoomId > 0)
    //        {
    //            LoadParticipants();
    //        }
    //    }
    //}
}