using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TrackSync.DataBase;

using TrackSync.DataBase.WorkWithDB;
using TrackSync.Interfaces;
using TrackSync.Windows;

namespace TrackSync.Pages
{
    public partial class CatalogEquipmentsPage : Page
    {
        // Вспомогательный класс для отслеживания выбора
        public class EquipmentWithSelection
        {
            public Equipment Equipment { get; set; }
            public bool IsSelected { get; set; }
            public bool IsUserEquipment
            {
                get
                {
                    return Equipment.CreatorUserId == SessionManager.CurrentUserId;
                }
            }
            public System.Windows.Visibility ActionButtonsVisibility
            {
                get
                {
                    return IsUserEquipment ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
                }
            }

        }

        private List<EquipmentWithSelection> _equipmentItems;
        private List<EquipmentWithSelection> _filteredItems;
        private readonly IEquipmentCallback _callback;
        private bool _showOnlyMyItems = false;

        public CatalogEquipmentsPage(IEquipmentCallback callback = null)
        {
            InitializeComponent();
            _callback = callback;
            LoadEquipment();
            UpdateFilterButtons();
        }

        /// Загрузка снаряжений из базы данных с учётом фильтрации
        /// </summary>
        private void LoadEquipment()
        {
            List<Equipment> equipmentList;

            if (_showOnlyMyItems)
            {
                // Показываем только снаряжение, созданное текущим пользователем
                equipmentList = TrackSyncManagerList.GetUserEquipment(SessionManager.CurrentUserId);
            }
            else
            {
                // Показываем все снаряжение (системное + созданное пользователем)
                equipmentList = TrackSyncManagerList.GetAllEquipmentForUser(SessionManager.CurrentUserId);
            }

            if (equipmentList != null)
            {
                _equipmentItems = equipmentList
                    .ConvertAll(e => new EquipmentWithSelection
                    {
                        Equipment = e,
                        IsSelected = false
                    });

                _filteredItems = new List<EquipmentWithSelection>(_equipmentItems);
                EquipmentsListBox.ItemsSource = _filteredItems;
            }
            else
            {
                // Если нет снаряжения, показываем пустое сообщение
                var emptyList = new List<EquipmentWithSelection>();
                EquipmentsListBox.ItemsSource = emptyList;
            }
        }

        /// <summary>
        /// Обработка клика по кнопке "Все"
        /// </summary>
        private void AllButton_Click(object sender, RoutedEventArgs e)
        {
            _showOnlyMyItems = false;
            UpdateFilterButtons();
            LoadEquipment();
        }

        /// <summary>
        /// Обработка клика по кнопке "Мои"
        /// </summary>
        private void MyButton_Click(object sender, RoutedEventArgs e)
        {
            _showOnlyMyItems = true;
            UpdateFilterButtons();
            LoadEquipment();
        }

        /// <summary>
        /// Обновление состояния кнопок фильтрации
        /// </summary>
        private void UpdateFilterButtons()
        {
            if (AllButton != null && MyButton != null)
            {
                if (_showOnlyMyItems)
                {
                    // Кнопка "Мои" активна
                    MyButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3498DB"));
                    MyButton.Foreground = new SolidColorBrush(Colors.White);
                    MyButton.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3498DB"));

                    // Кнопка "Все" неактивна
                    AllButton.Background = Brushes.Transparent;
                    AllButton.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2C3E50"));
                    AllButton.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E9ECEF"));
                }
                else
                {
                    // Кнопка "Все" активна
                    AllButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3498DB"));
                    AllButton.Foreground = new SolidColorBrush(Colors.White);
                    AllButton.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3498DB"));

                    // Кнопка "Мои" неактивна
                    MyButton.Background = Brushes.Transparent;
                    MyButton.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2C3E50"));
                    MyButton.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E9ECEF"));
                }
            }
        }

        /// <summary>
        /// Обработка поиска
        /// </summary>
        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_equipmentItems == null)
                return;

            string searchText = SearchTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(searchText))
            {
                ErrorMessageHide();
                _filteredItems = new List<EquipmentWithSelection>(_equipmentItems);
            }
            else
            {
                ErrorMessageHide();
                _filteredItems = _equipmentItems
                    .FindAll(eq => eq.Equipment.Title.ToLower().Contains(searchText.ToLower()));
            }

            EquipmentsListBox.ItemsSource = _filteredItems;
        }

        /// <summary>
        /// Обработка клика по элементу снаряжения
        /// </summary>
        private void EquipmentItem_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is EquipmentWithSelection item)
            {
                // Переключаем состояние выбора
                item.IsSelected = !item.IsSelected;

                ErrorMessageHide();

                // Обновляем интерфейс
                EquipmentsListBox.Items.Refresh();
            }
        }

        private void AddSelectedButton_Click(object sender, RoutedEventArgs e)
        {
            var selected = _equipmentItems?.FindAll(eq => eq.IsSelected) ?? new List<EquipmentWithSelection>();

            if (selected.Count == 0)
            {
                ErrorMessageVisible("*Пожалуйста, выберите хотя бы один ингредиент");
                return;
            }

            // Передаём выбранное снаряжение через коллбэк
            _callback?.OnEquipmentSelected(selected.Select(eq => eq.Equipment).ToList());

            // Возвращаемся назад
            if (NavigationService.CanGoBack)
                NavigationService.GoBack();
        }

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

        private void CreateEquipmentButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new CreateEquipmentPage());
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService.CanGoBack)
                NavigationService.GoBack();
        }

        /// <summary>
        /// Обработка клика по кнопке редактирования
        /// </summary>
        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is EquipmentWithSelection item)
            {
                // Блокируем редактирование системных объектов (на всякий случай)
                if (!item.IsUserEquipment)
                {
                    MessageBox.Show("Вы не можете редактировать системное снаряжение.", "Доступ запрещён",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Навигация на страницу редактирования
                NavigationService.Navigate(new CreateEquipmentPage(item.Equipment));
            }
        }

        /// <summary>
        /// Обработка клика по кнопке удаления
        /// </summary>
        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is EquipmentWithSelection item)
            {
                DeleteEquipment(item.Equipment);
            }
        }

        /// <summary>
        /// Удаление снаряжения
        /// </summary>
        private void DeleteEquipment(Equipment equipment)
        {
            try
            {
                using (var ctx = new TrackSyncContext())
                {
                    var equipmentToRemove = ctx.Equipments.FirstOrDefault(e => e.EquipmentId == equipment.EquipmentId);

                    if (equipmentToRemove != null)
                    { 
                        // Просто удаляем - каскадное удаление произойдет автоматически
                        ctx.Equipments.Remove(equipmentToRemove);
                        ctx.SaveChanges();
                        LoadEquipment();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении:\n{ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}