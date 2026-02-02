using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using TrackSync.DataBase;
//using TrackSync.DataBase.Model;
using TrackSync.DataBase.WorkWithDB;
using TrackSync.Windows;

namespace TrackSync.Pages
{
    public partial class CreateEquipmentPage : Page
    {
        private readonly Equipment _editingEquipment; // Снаряжение для редактирования (если есть)
        private readonly bool _isEditMode; // Флаг режима редактирования

        /// <summary>
        /// Конструктор для создания НОВОГО снаряжения
        /// </summary>
        public CreateEquipmentPage()
        {
            InitializeComponent();
            _isEditMode = false;
            InitializePage();
        }

        /// <summary>
        /// Конструктор для РЕДАКТИРОВАНИЯ существующего снаряжения
        /// </summary>
        public CreateEquipmentPage(Equipment equipment)
        {
            InitializeComponent();
            _editingEquipment = equipment;
            _isEditMode = true;
            InitializePage();
        }

        /// <summary>
        /// Инициализация страницы (заголовок и поля)
        /// </summary>
        private void InitializePage()
        {
            if (_isEditMode)
            {
                // Режим РЕДАКТИРОВАНИЯ
                // Изменяем заголовок страницы
                var titleBlock = this.FindName("TitleText") as TextBlock;
                if (titleBlock != null)
                {
                    // Ищем TextBlock с заголовком
                    foreach (var child in LogicalTreeHelper.GetChildren(this))
                    {
                        if (child is DependencyObject depObj)
                        {
                            var textBlocks = FindVisualChildren<TextBlock>(depObj);
                            foreach (var tb in textBlocks)
                            {
                                if (tb.FontSize == 28 && tb.FontWeight == FontWeights.Bold)
                                {
                                    tb.Text = "Редактирование снаряжения";
                                    break;
                                }
                            }
                        }
                    }
                }

                // Меняем текст кнопки
                var createButtonContent = CreateButton.Content as StackPanel;
                if (createButtonContent != null)
                {
                    foreach (var child in createButtonContent.Children)
                    {
                        if (child is TextBlock tb)
                        {
                            tb.Text = "Сохранить изменения";
                            break;
                        }
                    }
                }

                // Заполняем поля данными для редактирования
                if (_editingEquipment != null)
                {
                    NameTextBox.Text = _editingEquipment.Title;
                    WeightTextBox.Text = _editingEquipment.Weight.ToString();
                    DescriptionTextBox.Text = _editingEquipment.Description ?? string.Empty;
                }
            }
            else
            {
                // Режим СОЗДАНИЯ - всё по умолчанию
            }
        }

        /// <summary>
        /// Вспомогательный метод для поиска дочерних элементов
        /// </summary>
        private static System.Collections.Generic.IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T t)
                    {
                        yield return t;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
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

        // Ввод - только цифры для поля веса
        private void WeightTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
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

        // Сохранение снаряжения (создание или редактирование)
        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorMessageHide();

            if (!ValidateForm())
                return;

            try
            {
                using (var ctx = new TrackSyncContext())
                {
                    if (_isEditMode)
                    {
                        // ===== РЕДАКТИРОВАНИЕ =====
                        var equipmentToUpdate = ctx.Equipments
                            .FirstOrDefault(e => e.EquipmentId == _editingEquipment.EquipmentId);

                        if (equipmentToUpdate == null)

                            // Обновляем данные
                            equipmentToUpdate.Title = NameTextBox.Text.Trim();
                        equipmentToUpdate.Weight = int.Parse(WeightTextBox.Text);
                        equipmentToUpdate.Description = string.IsNullOrWhiteSpace(DescriptionTextBox.Text)
                            ? null
                            : DescriptionTextBox.Text.Trim();

                        ctx.SaveChanges();
                    }
                    else
                    {
                        // ===== СОЗДАНИЕ =====
                        var newEquipment = new Equipment(
                            title: NameTextBox.Text.Trim(),
                            weight: int.Parse(WeightTextBox.Text),
                            creatorUserId: SessionManager.CurrentUserId,
                            description: string.IsNullOrWhiteSpace(DescriptionTextBox.Text)
                                ? null
                                : DescriptionTextBox.Text.Trim()
                        );

                        ctx.Equipments.Add(newEquipment);
                        ctx.SaveChanges();
                    }

                    // Возвращаемся в каталог
                    if (FrameContext.MainWindowFrame.CanGoBack)
                        FrameContext.MainWindowFrame.GoBack();
                }
            }
            catch (Exception ex)
            {
                ErrorMessageVisible($"*Ошибка при сохранении снаряжения: {ex.Message}");
            }
        }

        // Отмена
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (FrameContext.MainWindowFrame.CanGoBack)
                FrameContext.MainWindowFrame.GoBack();
        }

        // Валидация формы
        private bool ValidateForm()
        {
            // Проверка названия
            if (string.IsNullOrWhiteSpace(NameTextBox?.Text))
            {
                ErrorMessageVisible("*Пожалуйста, введите название снаряжения");
                return false;
            }

            if (NameTextBox.Text.Length > 256)
            {
                ErrorMessageVisible("*Название не должно превышать 256 символов");
                return false;
            }

            // Проверка веса
            if (string.IsNullOrWhiteSpace(WeightTextBox?.Text))
            {
                ErrorMessageVisible("*Пожалуйста, укажите вес снаряжения");
                return false;
            }

            if (!int.TryParse(WeightTextBox.Text, out int weight))
            {
                ErrorMessageVisible("*Вес должен быть целым числом");
                return false;
            }

            if (weight <= 0)
            {
                ErrorMessageVisible("*Вес должен быть положительным числом");
                return false;
            }

            // Проверка описания (необязательное поле)
            if (!string.IsNullOrWhiteSpace(DescriptionTextBox?.Text) && DescriptionTextBox.Text.Length > 1000)
            {
                ErrorMessageVisible("*Описание не должно превышать 1000 символов");
                return false;
            }

            return true;
        }
    }
}