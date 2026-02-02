using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
//using TrackSync.DataBase.Model;
using TrackSync.DataBase.WorkWithDB;
using TrackSync.Interfaces;
using TrackSync.Windows;

namespace TrackSync.Pages
{
    public partial class HikeRoomPage : Page, IEquipmentCallback
    {
        private int _roomId;
        private int _currentDay = 1;
        private int _currentParticipantId = 1;
        private readonly Dictionary<int, List<EquipmentQuantityItem>> _participantEquipment = new();

        // Вспомогательный класс для хранения снаряжения с количеством
        private class EquipmentQuantityItem
        {
            public Equipment Equipment { get; set; }
            public int Quantity { get; set; }
        }

        public HikeRoomPage(int roomId = 1)
        {
            InitializeComponent();
            _roomId = roomId;
            LoadRoomData();
            ActivateDayButton(1);
        }

        // Реализация интерфейса для получения выбранного снаряжения
        public void OnEquipmentSelected(List<Equipment> equipment)
        {
            if (equipment == null || equipment.Count == 0)
                return;

            // Получаем или создаём список снаряжения для участника
            if (!_participantEquipment.TryGetValue(_currentParticipantId, out var participantList))
            {
                participantList = new List<EquipmentQuantityItem>();
                _participantEquipment[_currentParticipantId] = participantList;
            }

            // Добавляем новые снаряжения (без дубликатов)
            foreach (var eq in equipment)
            {
                if (!participantList.Exists(e => e.Equipment.EquipmentId == eq.EquipmentId))
                {
                    participantList.Add(new EquipmentQuantityItem
                    {
                        Equipment = eq,
                        Quantity = 1 // Значение по умолчанию
                    });
                }
            }

            // Обновляем отображение для текущего участника
            UpdateEquipmentUI(_currentParticipantId);
        }

        // Обновление интерфейса снаряжения для участника
        private void UpdateEquipmentUI(int participantId)
        {
            // Находим стек панель для снаряжения по ID участника
            var equipmentStackPanel = FindEquipmentStackPanel(participantId);
            if (equipmentStackPanel == null)
                return;

            // Очищаем текущий список
            equipmentStackPanel.Children.Clear();

            // Получаем список снаряжения для участника
            if (_participantEquipment.TryGetValue(participantId, out var equipmentList) && equipmentList.Any())
            {
                // Добавляем каждое снаряжение в интерфейс
                foreach (var item in equipmentList)
                {
                    var equipmentPanel = CreateEquipmentPanel(item);
                    equipmentStackPanel.Children.Add(equipmentPanel);
                }

                // Обновляем статистику
                UpdateEquipmentStats(participantId, equipmentList);
            }
            else
            {
                // Показываем сообщение "Нет снаряжения"
                var emptyText = new TextBlock
                {
                    Style = (Style)FindResource("BodyText"),
                    Foreground = (Brush)new BrushConverter().ConvertFrom("#7F8C8D"),
                    Text = "Нет добавленного снаряжения",
                    Margin = new Thickness(0, 5, 0, 0)
                };
                equipmentStackPanel.Children.Add(emptyText);

                // Сбрасываем статистику
                UpdateEquipmentStats(participantId, new List<EquipmentQuantityItem>());
            }
        }


        // Создание панели для отображения снаряжения
        private StackPanel CreateEquipmentPanel(EquipmentQuantityItem item)
        {
            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 8),
                Tag = item
            };

            // Название снаряжения
            var nameText = new TextBlock
            {
                Text = item.Equipment.Title,
                Style = (Style)FindResource("BodyText"),
                VerticalAlignment = VerticalAlignment.Center,
                Width = 220,
                TextWrapping = TextWrapping.Wrap
            };
            panel.Children.Add(nameText);

            // Поле для ввода количества со стилем InputField
            var quantityTextBox = new TextBox
            {
                Width = 80,
                Height = 36,
                Margin = new Thickness(10, 0, 0, 0),
                Text = item.Quantity.ToString(),
                Tag = item,
                Style = (Style)FindResource("InputField")
            };
            quantityTextBox.TextChanged += QuantityTextBox_TextChanged;
            quantityTextBox.PreviewTextInput += QuantityTextBox_PreviewTextInput;
            panel.Children.Add(quantityTextBox);

            // Текст "шт."
            var unitText = new TextBlock
            {
                Text = "шт.",
                Style = (Style)FindResource("BodyText"),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(5, 0, 0, 0),
                Foreground = (Brush)new BrushConverter().ConvertFrom("#7F8C8D")
            };
            panel.Children.Add(unitText);

            // Вес одного предмета
            var weightText = new TextBlock
            {
                Text = $"({item.Equipment.Weight}г)",
                Style = (Style)FindResource("BodyText"),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 0, 0, 0),
                Foreground = (Brush)new BrushConverter().ConvertFrom("#7F8C8D"),
                FontSize = 12
            };
            panel.Children.Add(weightText);

            // Кнопка удаления с иконкой корзины
            var removeButton = new Button
            {
                Width = 36,
                Height = 36,
                Style = (Style)FindResource("IconButton"),
                Margin = new Thickness(10, 0, 0, 0),
                Tag = panel,
                ToolTip = "Удалить снаряжение"
            };

            var image = new Image
            {
                Source = new BitmapImage(new Uri("/Images/Icons/trash.png", UriKind.Relative)),
                Width = 18,
                Height = 18
            };
            removeButton.Content = image;

            removeButton.Click += RemoveEquipmentButton_Click;
            panel.Children.Add(removeButton);

            return panel;
        }

        // Обработчик изменения количества
        private void QuantityTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox && textBox.Tag is EquipmentQuantityItem item)
            {
                // Сохраняем текущее значение для восстановления в случае ошибки
                int previousQuantity = item.Quantity;

                // Обработка пустой строки — интерпретируем как 0
                if (string.IsNullOrWhiteSpace(textBox.Text))
                {
                    item.Quantity = 0;

                    // Автоматически обновляем статистику
                    if (_participantEquipment.TryGetValue(_currentParticipantId, out var equipmentList))
                    {
                        UpdateEquipmentStats(_currentParticipantId, equipmentList);
                    }
                    return;
                }

                // Попытка преобразовать в число
                if (int.TryParse(textBox.Text, out int quantity))
                {
                    // Разрешаем 0 и положительные числа
                    if (quantity >= 0)
                    {
                        item.Quantity = quantity;

                        // Автоматически обновляем статистику
                        if (_participantEquipment.TryGetValue(_currentParticipantId, out var equipmentList))
                        {
                            UpdateEquipmentStats(_currentParticipantId, equipmentList);
                        }
                    }
                    else
                    {
                        // Восстанавливаем предыдущее значение при отрицательном числе
                        textBox.Text = previousQuantity.ToString();
                        textBox.SelectionStart = textBox.Text.Length;
                    }
                }
                else
                {
                    // Восстанавливаем предыдущее значение при некорректном вводе
                    textBox.Text = previousQuantity.ToString();
                    textBox.SelectionStart = textBox.Text.Length;
                }
            }
        }

        // Валидация ввода (только цифры)
        private void QuantityTextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
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

        // Удаление снаряжения
        private void RemoveEquipmentButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is StackPanel panel && panel.Tag is EquipmentQuantityItem item)
            {
                if (_participantEquipment.TryGetValue(_currentParticipantId, out var equipmentList))
                {
                    equipmentList.Remove(item);
                    UpdateEquipmentUI(_currentParticipantId);
                }
            }
        }

        // Поиск стек панели для снаряжения по ID участника в ItemsControl
        private StackPanel FindEquipmentStackPanel(int participantId)
        {
            foreach (var item in ParticipantsItemsControl.Items)
            {
                var container = ParticipantsItemsControl.ItemContainerGenerator.ContainerFromItem(item);
                if (container != null)
                {
                    var panel = FindVisualChild<StackPanel>(container,
                        sp => sp.Tag?.ToString() == participantId.ToString());
                    if (panel != null)
                        return panel;
                }
            }
            return null;
        }

        // Обновление статистики снаряжения
        private void UpdateEquipmentStats(int participantId, List<EquipmentQuantityItem> equipmentList)
        {
            // Вычисляем общий вес и количество предметов
            int totalWeight = 0;
            int totalItems = 0;

            foreach (var item in equipmentList)
            {
                totalWeight += item.Equipment.Weight * item.Quantity;
                totalItems += item.Quantity;
            }

            // Ищем элементы статистики для данного участника
            foreach (var item in ParticipantsItemsControl.Items)
            {
                var container = ParticipantsItemsControl.ItemContainerGenerator.ContainerFromItem(item);
                if (container != null)
                {
                    var weightText = FindVisualChild<TextBlock>(container, tb => tb.Name == "TotalWeightText");
                    var itemsText = FindVisualChild<TextBlock>(container, tb => tb.Name == "TotalItemsText");

                    if (weightText != null)
                        weightText.Text = $"{totalWeight / 1000.0:F1} кг";

                    if (itemsText != null)
                        itemsText.Text = $"{totalItems} шт.";
                }
            }
        }

        // Вспомогательный метод для поиска дочернего элемента с предикатом
        private T FindVisualChild<T>(DependencyObject parent, Func<T, bool> predicate) where T : DependencyObject
        {
            if (parent == null) return null;

            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is T typedChild && predicate(typedChild))
                    return typedChild;

                var result = FindVisualChild<T>(child, predicate);
                if (result != null)
                    return result;
            }

            return null;
        }

        // Вспомогательный метод для поиска дочернего элемента (без предиката)
        private T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) return null;

            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is T typedChild)
                    return typedChild;

                var result = FindVisualChild<T>(child);
                if (result != null)
                    return result;
            }

            return null;
        }

        // Обработчик кнопки "Добавить снаряжение"
        private void AddEquipmentButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int participantId)
            {
                _currentParticipantId = participantId;

                // Проверяем, что NavigationService доступен
                if (NavigationService == null)
                {
                    MessageBox.Show("Ошибка навигации: NavigationService недоступен", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                NavigationService.Navigate(new CatalogEquipmentsPage(this));
            }
        }

        private void LoadRoomData()
        {
            // Загружаем название комнаты из БД (пока заглушка)
            RoomTitleText.Text = "Поход на Эльбрус";

            // Загружаем участников комнаты со снаряжением
            LoadParticipants();
        }

        // Загрузка участников комнаты
        // Загрузка участников комнаты со снаряжением
        private void LoadParticipants()
        {
            var participants = TrackSyncManagerList.GetParticipantsWithEquipment(_roomId);

            if (participants != null && participants.Any())
            {
                ParticipantsItemsControl.ItemsSource = participants;

                // Инициализируем словарь снаряжения для каждого участника и загружаем данные из БД
                foreach (var participant in participants)
                {
                    var equipmentList = new List<EquipmentQuantityItem>();

                    // Загружаем снаряжение из таблицы HikeRoomsEquipment
                    foreach (var hre in participant.HikeRoomsEquipments)
                    {

                        if (hre.Equipment != null)
                        {
                            equipmentList.Add(new EquipmentQuantityItem
                            {
                                Equipment = hre.Equipment,
                                Quantity = hre.Quantity
                            });
                        }
                    }

                    _participantEquipment[participant.ParticipantId] = equipmentList;

                    // Обновляем интерфейс для этого участника
                    UpdateEquipmentUI(participant.ParticipantId);
                }
            }
            else
            {
                MessageBox.Show("В комнате нет участников. Пригласите пользователей!", "Информация",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // === Вкладка 1: Маршруты ===
        private void CreateRouteButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Функция создания маршрута будет реализована в будущих версиях",
                          "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void EditRouteButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Функция редактирования маршрута будет реализована в будущих версиях",
                          "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // === Вкладка 3: Продуктовая раскладка ===
        private void DayButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is string dayStr && int.TryParse(dayStr, out int day))
            {
                SwitchDay(day);
            }
        }

        private void SwitchDay(int day)
        {
            // Скрываем все дни
            Day1Content.Visibility = Visibility.Collapsed;
            Day2Content.Visibility = Visibility.Collapsed;
            // Day3Content.Visibility = Visibility.Collapsed; // Добавить при необходимости

            // Показываем выбранный день
            switch (day)
            {
                case 1:
                    Day1Content.Visibility = Visibility.Visible;
                    break;
                case 2:
                    Day2Content.Visibility = Visibility.Visible;
                    break;
                    // case 3:
                    //     Day3Content.Visibility = Visibility.Visible;
                    //     break;
            }

            _currentDay = day;
            ActivateDayButton(day);
        }

        private void ActivateDayButton(int day)
        {
            // Сбрасываем все кнопки
            Day1Button.Background = System.Windows.Media.Brushes.White;
            Day1Button.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(44, 62, 80));
            Day2Button.Background = System.Windows.Media.Brushes.White;
            Day2Button.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(44, 62, 80));
            // Day3Button.Background = System.Windows.Media.Brushes.White;
            // Day3Button.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(44, 62, 80));

            // Активируем выбранную кнопку
            switch (day)
            {
                case 1:
                    Day1Button.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(52, 152, 219));
                    Day1Button.Foreground = System.Windows.Media.Brushes.White;
                    break;
                case 2:
                    Day2Button.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(52, 152, 219));
                    Day2Button.Foreground = System.Windows.Media.Brushes.White;
                    break;
                    // case 3:
                    //     Day3Button.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(52, 152, 219));
                    //     Day3Button.Foreground = System.Windows.Media.Brushes.White;
                    //     break;
            }
        }

        private void AddDayButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Добавление нового дня будет реализовано в будущих версиях",
                          "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void AddRecipeButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            string mealType = button?.Tag?.ToString() ?? "unknown";

            MessageBox.Show($"Откроется каталог рецептов для добавления в {GetMealTypeName(mealType)}",
                          "Добавление рецепта", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private string GetMealTypeName(string tag)
        {
            return tag switch
            {
                "breakfast" => "завтрак",
                "lunch" => "обед",
                "snack" => "перекус",
                "dinner" => "ужин",
                "day2_breakfast" => "завтрак (день 2)",
                "day2_lunch" => "обед (день 2)",
                "day2_snack" => "перекус (день 2)",
                "day2_dinner" => "ужин (день 2)",
                _ => "неизвестный приём пищи"
            };
        }

        // === Дополнительные кнопки ===
        private void InviteUsersButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Откроется окно для приглашения пользователей в комнату",
                          "Приглашение пользователей", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void RoomSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Откроются настройки комнаты:\n- Изменение названия\n- Смена статуса\n- Управление доступом\n- Удаление комнаты",
                          "Настройки комнаты", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}