using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;
using TrackSync.DataBase;
using TrackSync.DataBase.Model;
using TrackSync.DataBase.WorkWithDB;
using TrackSync.Interfaces;
using TrackSync.Pages;

namespace TrackSync.Controls
{
    public partial class EquipmentControl : UserControl, IEquipmentCallback
    {
        private class EquipmentQuantityItem
        {
            public Equipment Equipment { get; set; }
            public int Quantity { get; set; }
        }

        public event EventHandler EquipmentSaved;
        public event EventHandler<int> AddEquipmentRequested;
        private readonly Dictionary<int, StackPanel> _participantStackPanels = new();

        public static readonly DependencyProperty RoomIdProperty =
            DependencyProperty.Register("RoomId", typeof(int), typeof(EquipmentControl),
                new PropertyMetadata(0, OnRoomIdChanged));

        public static readonly DependencyProperty CanEditEquipmentProperty =
            DependencyProperty.Register("CanEditEquipment", typeof(bool), typeof(EquipmentControl),
                new PropertyMetadata(false, OnCanEditEquipmentChanged));

        public int RoomId
        {
            get => (int)GetValue(RoomIdProperty);
            set => SetValue(RoomIdProperty, value);
        }

        public bool CanEditEquipment
        {
            get => (bool)GetValue(CanEditEquipmentProperty);
            set => SetValue(CanEditEquipmentProperty, value);
        }

        private int _currentParticipantId = 1;
        private readonly Dictionary<int, List<EquipmentQuantityItem>> _participantEquipment = new();

        public int CurrentParticipantId
        {
            get => _currentParticipantId;
            set => _currentParticipantId = value;
        }

        public EquipmentControl()
        {
            InitializeComponent();
            Loaded += EquipmentControl_Loaded;
        }

        private void EquipmentControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (RoomId > 0)
            {
                LoadParticipants();
            }
        }

        private static void OnRoomIdChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (EquipmentControl)d;
            if (control.IsLoaded && (int)e.NewValue > 0)
            {
                control.LoadParticipants();
            }
        }

        private static void OnCanEditEquipmentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (EquipmentControl)d;
            if (control.IsLoaded)
            {
                control.ApplyPermissions();
            }
        }

        private void LoadParticipants()
        {
            try
            {
                var participants = TrackSyncManagerList.GetParticipantsWithEquipment(RoomId);

                if (participants != null && participants.Any())
                {
                    _participantEquipment.Clear();

                    foreach (var participant in participants)
                    {
                        var equipmentList = new List<EquipmentQuantityItem>();
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
                    }

                    ParticipantsItemsControl.ItemsSource = participants;

                    ParticipantsItemsControl.Loaded += (s, e) =>
                    {
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            foreach (var participant in participants)
                            {
                                UpdateEquipmentUI(participant.ParticipantId);
                            }
                            ApplyPermissions();
                        }), System.Windows.Threading.DispatcherPriority.Background);
                    };

                    ParticipantsItemsControl.ItemContainerGenerator.StatusChanged += (s, e) =>
                    {
                        if (ParticipantsItemsControl.ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated)
                        {
                            _participantStackPanels.Clear();
                            foreach (var item in participants)
                            {
                                var container = ParticipantsItemsControl.ItemContainerGenerator.ContainerFromItem(item);
                                if (container is null) continue;
                                var stackPanel = FindVisualChildByName<StackPanel>(container, "EquipmentStackPanel");
                                if (stackPanel != null)
                                {
                                    _participantStackPanels[item.ParticipantId] = stackPanel;
                                }
                            }
                            foreach (var kvp in _participantEquipment)
                                UpdateEquipmentUI(kvp.Key);
                        }
                    };
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки участников: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateEquipmentUI(int participantId)
        {
            if (!_participantStackPanels.TryGetValue(participantId, out var equipmentStackPanel))
            {
                Dispatcher.BeginInvoke(new Action(() => UpdateEquipmentUI(participantId)),
                                       DispatcherPriority.Render);
                return;
            }

            if (equipmentStackPanel == null)
                return;

            equipmentStackPanel.Children.Clear();

            if (_participantEquipment.TryGetValue(participantId, out var equipmentList) && equipmentList.Any())
            {
                foreach (var item in equipmentList)
                {
                    var equipmentPanel = CreateEquipmentPanel(item, participantId);
                    equipmentStackPanel.Children.Add(equipmentPanel);
                }

                UpdateEquipmentStats(participantId, equipmentList);
            }
            else
            {
                var emptyText = new TextBlock
                {
                    Style = (Style)FindResource("BodyText"),
                    Foreground = (Brush)new BrushConverter().ConvertFrom("#7F8C8D"),
                    Text = "Нет добавленного снаряжения",
                    Margin = new Thickness(0, 5, 0, 0)
                };
                equipmentStackPanel.Children.Add(emptyText);
                UpdateEquipmentStats(participantId, new List<EquipmentQuantityItem>());
            }
        }

        private StackPanel CreateEquipmentPanel(EquipmentQuantityItem item, int participantId)
        {
            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 8),
                Tag = item
            };

            var nameText = new TextBlock
            {
                Text = item.Equipment.Title,
                Style = (Style)FindResource("BodyText"),
                VerticalAlignment = VerticalAlignment.Center,
                Width = 220,
                TextWrapping = TextWrapping.Wrap
            };
            panel.Children.Add(nameText);

            var quantityTextBox = new TextBox
            {
                Width = 80,
                Height = 36,
                Margin = new Thickness(10, 0, 0, 0),
                Text = item.Quantity.ToString(),
                Tag = item,
                Style = (Style)FindResource("InputField")
            };

            if (CanEditEquipment)
            {
                quantityTextBox.TextChanged += QuantityTextBox_TextChanged;
                quantityTextBox.PreviewTextInput += QuantityTextBox_PreviewTextInput;
            }
            else
            {
                quantityTextBox.IsReadOnly = true;
                quantityTextBox.Background = Brushes.LightGray;
            }

            panel.Children.Add(quantityTextBox);

            var unitText = new TextBlock
            {
                Text = "шт.",
                Style = (Style)FindResource("BodyText"),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(5, 0, 0, 0),
                Foreground = (Brush)new BrushConverter().ConvertFrom("#7F8C8D")
            };
            panel.Children.Add(unitText);

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

            if (CanEditEquipment)
            {
                var removeButton = new Button
                {
                    Width = 36,
                    Height = 36,
                    Style = (Style)FindResource("IconButton"),
                    Margin = new Thickness(10, 0, 0, 0),
                    Tag = new { Item = item, ParticipantId = participantId },
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
            }

            return panel;
        }

        private void QuantityTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox && textBox.Tag is EquipmentQuantityItem item)
            {
                int previousQuantity = item.Quantity;

                if (string.IsNullOrWhiteSpace(textBox.Text))
                {
                    item.Quantity = 0;

                    if (_participantEquipment.TryGetValue(_currentParticipantId, out var equipmentList))
                    {
                        UpdateEquipmentStats(_currentParticipantId, equipmentList);
                    }
                    return;
                }

                if (int.TryParse(textBox.Text, out int quantity))
                {
                    if (quantity >= 0)
                    {
                        item.Quantity = quantity;

                        if (_participantEquipment.TryGetValue(_currentParticipantId, out var equipmentList))
                        {
                            UpdateEquipmentStats(_currentParticipantId, equipmentList);
                        }
                    }
                    else
                    {
                        textBox.Text = previousQuantity.ToString();
                        textBox.SelectionStart = textBox.Text.Length;
                    }
                }
                else
                {
                    textBox.Text = previousQuantity.ToString();
                    textBox.SelectionStart = textBox.Text.Length;
                }
            }
        }

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

        private void RemoveEquipmentButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button &&
                button.Tag is object tagObj &&
                tagObj.GetType().GetProperty("Item")?.GetValue(tagObj) is EquipmentQuantityItem item &&
                tagObj.GetType().GetProperty("ParticipantId")?.GetValue(tagObj) is int participantId)
            {
                if (_participantEquipment.TryGetValue(participantId, out var equipmentList))
                {
                    equipmentList.Remove(item);
                    UpdateEquipmentUI(participantId);
                }
            }
        }

        private void UpdateEquipmentStats(int participantId, List<EquipmentQuantityItem> equipmentList)
        {
            int totalWeight = 0;
            int totalItems = 0;

            foreach (var item in equipmentList)
            {
                totalWeight += item.Equipment.Weight * item.Quantity;
                totalItems += item.Quantity;
            }

            var container = ParticipantsItemsControl.ItemContainerGenerator.ContainerFromItem(
                ParticipantsItemsControl.Items.OfType<Participant>().FirstOrDefault(p => p.ParticipantId == participantId));

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

        private StackPanel FindEquipmentStackPanel(int participantId)
        {
            foreach (var item in ParticipantsItemsControl.Items)
            {
                var container = ParticipantsItemsControl.ItemContainerGenerator.ContainerFromItem(item);
                if (container == null) continue;

                var panels = FindAllVisualChildren<StackPanel>(container);
                foreach (var panel in panels)
                {
                    if (panel.Tag is int id && id == participantId)
                        return panel;
                }
            }
            return null;
        }

        private T FindVisualChildByName<T>(DependencyObject parent, string name) where T : FrameworkElement
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild && typedChild.Name == name)
                    return typedChild;
                var descendant = FindVisualChildByName<T>(child, name);
                if (descendant != null)
                    return descendant;
            }
            return null;
        }

        private List<T> FindAllVisualChildren<T>(DependencyObject parent) where T : DependencyObject
        {
            var children = new List<T>();
            if (parent == null) return children;

            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is T typedChild)
                    children.Add(typedChild);

                children.AddRange(FindAllVisualChildren<T>(child));
            }

            return children;
        }

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

        private void AddEquipmentButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int participantId)
            {
                _currentParticipantId = participantId;
                AddEquipmentRequested?.Invoke(this, participantId);
            }
        }

        private void SaveChangesButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var ctx = new TrackSyncContext())
                {
                    int addedCount = 0;
                    int updatedCount = 0;
                    int deletedCount = 0;

                    var existingRecords = ctx.HikeRoomsEquipments
                        .Where(hre => hre.HikeRoomId == RoomId)
                        .ToList();

                    foreach (var item in ParticipantsItemsControl.Items)
                    {
                        if (item is Participant participant)
                        {
                            int participantId = participant.ParticipantId;

                            if (_participantEquipment.TryGetValue(participantId, out var equipmentList))
                            {
                                foreach (var eqItem in equipmentList)
                                {
                                    if (eqItem.Quantity <= 0)
                                        continue;

                                    var existingRecord = existingRecords
                                        .FirstOrDefault(hre =>
                                            hre.ParticipantId == participantId &&
                                            hre.EquipmentId == eqItem.Equipment.EquipmentId);

                                    if (existingRecord != null)
                                    {
                                        existingRecord.Quantity = eqItem.Quantity;
                                        updatedCount++;
                                    }
                                    else
                                    {
                                        var newRecord = new HikeRoomsEquipment
                                        {
                                            HikeRoomId = RoomId,
                                            ParticipantId = participantId,
                                            EquipmentId = eqItem.Equipment.EquipmentId,
                                            Quantity = eqItem.Quantity
                                        };
                                        ctx.HikeRoomsEquipments.Add(newRecord);
                                        addedCount++;
                                    }
                                }

                                var recordsToDelete = existingRecords
                                    .Where(hre => hre.ParticipantId == participantId &&
                                                 !equipmentList.Any(eq =>
                                                     eq.Equipment.EquipmentId == hre.EquipmentId &&
                                                     eq.Quantity > 0))
                                    .ToList();

                                foreach (var record in recordsToDelete)
                                {
                                    ctx.HikeRoomsEquipments.Remove(record);
                                    deletedCount++;
                                }
                            }
                        }
                    }

                    ShowMessage("Изменения сохранены", true);
                    ctx.SaveChanges();

                    EquipmentSaved?.Invoke(this, EventArgs.Empty);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении снаряжения:\n{ex}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyPermissions()
        {
            SaveChangesButton.Visibility = CanEditEquipment ? Visibility.Visible : Visibility.Collapsed;

            foreach (var item in ParticipantsItemsControl.Items)
            {
                var container = ParticipantsItemsControl.ItemContainerGenerator.ContainerFromItem(item);
                if (container == null) continue;

                var addButton = FindVisualChild<Button>(container, btn =>
                    btn.ToolTip != null && btn.ToolTip.ToString() == "Добавить снаряжение");

                if (addButton != null)
                {
                    addButton.Visibility = CanEditEquipment ? Visibility.Visible : Visibility.Collapsed;
                }

                if (!CanEditEquipment)
                {
                    var removeButtons = FindAllVisualChildren<Button>(container)
                        .Where(btn => btn.ToolTip?.ToString() == "Удалить снаряжение");

                    foreach (var btn in removeButtons)
                    {
                        btn.Visibility = Visibility.Collapsed;
                    }

                    var quantityTextBoxes = FindAllVisualChildren<TextBox>(container)
                        .Where(tb => tb.Tag is EquipmentQuantityItem);

                    foreach (var tb in quantityTextBoxes)
                    {
                        tb.IsReadOnly = true;
                        tb.Background = Brushes.LightGray;
                    }
                }
            }
        }

        private void ShowMessage(string message, bool isSuccess)
        {
            SaveSuccessMessage.Visibility = Visibility.Visible;
            SaveSuccessMessage.Text = message;
            SaveSuccessMessage.Foreground = isSuccess ? Brushes.Green : Brushes.Red;

            var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            timer.Tick += (s, args) =>
            {
                SaveSuccessMessage.Visibility = Visibility.Collapsed;
                timer.Stop();
            };
            timer.Start();
        }

        public void OnEquipmentSelected(List<Equipment> equipment)
        {
            if (equipment == null || equipment.Count == 0 || _currentParticipantId <= 0)
            {
                MessageBox.Show($"Ошибка: оборудование={equipment?.Count}, участник={_currentParticipantId}");
                return;
            }

            if (!_participantEquipment.TryGetValue(_currentParticipantId, out var participantList))
            {
                participantList = new List<EquipmentQuantityItem>();
                _participantEquipment[_currentParticipantId] = participantList;
            }

            foreach (var eq in equipment)
            {
                if (!participantList.Exists(e => e.Equipment.EquipmentId == eq.EquipmentId))
                {
                    participantList.Add(new EquipmentQuantityItem
                    {
                        Equipment = eq,
                        Quantity = 1
                    });
                }
            }

            Dispatcher.BeginInvoke(new Action(() => UpdateEquipmentUI(_currentParticipantId)),
                                   DispatcherPriority.Render);
        }
    }
}