using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using TrackSync.DataBase.WorkWithDB;
using TrackSync.DataBase;
using TrackSync.DataBase.Model;
using TrackSync.Windows;
using TrackSync.Functions;

namespace TrackSync.Pages
{
    public partial class CreateEquipmentPage : Page
    {
        private readonly Equipment _editingEquipment; 
        private readonly bool _isEditMode; 

        public CreateEquipmentPage()
        {
            InitializeComponent();
            _isEditMode = false;
            InitializePage();
        }

        public CreateEquipmentPage(Equipment equipment)
        {
            InitializeComponent();
            _editingEquipment = equipment;
            _isEditMode = true;
            InitializePage();
        }

        private void InitializePage()
        {
            if (_isEditMode)
            {
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
                }
            }
            else
            {

            }
        }


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
                        var equipmentToUpdate = ctx.Equipments
                            .FirstOrDefault(e => e.EquipmentId == _editingEquipment.EquipmentId);

                        if (equipmentToUpdate == null)

                            // Обновляем данные
                            equipmentToUpdate.Title = NameTextBox.Text.Trim();
                            equipmentToUpdate.Weight = int.Parse(WeightTextBox.Text);

                        ctx.SaveChanges();
                    }
                    else
                    {
                        var newEquipment = new Equipment(
                            title: NameTextBox.Text.Trim(),
                            weight: int.Parse(WeightTextBox.Text),
                            creatorUserId: SessionManager.CurrentUserId
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

            if (NameTextBox.Text.Length > 50)
            {
                ErrorMessageVisible("*Название не должно превышать 50 символов");
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
                ErrorMessageVisible("*Слишком большоё вес снаряжения");
                return false;
            }

            if (weight <= 0)
            {
                ErrorMessageVisible("*Вес должен быть положительным числом");
                return false;
            }
            return true;
        }
    }
}