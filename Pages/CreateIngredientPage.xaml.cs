using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using TrackSync.DataBase;
using TrackSync.DataBase.Model;
using TrackSync.DataBase.WorkWithDB;
using TrackSync.Functions;
using TrackSync.Windows;

namespace TrackSync.Pages
{
    public partial class CreateIngredientPage : Page
    {
        private readonly int? _editingIngredientId; // ID редактируемого ингредиента
        private readonly bool _isEditMode; // Флаг режима редактирования

        /// <summary>
        /// Конструктор для создания НОВОГО ингредиента
        /// </summary>
        public CreateIngredientPage()
        {
            InitializeComponent();
            _isEditMode = false;
            InitializePage();
        }

        /// <summary>
        /// Конструктор для РЕДАКТИРОВАНИЯ существующего ингредиента
        /// </summary>
        public CreateIngredientPage(Ingredient ingredient)
        {
            InitializeComponent();
            _editingIngredientId = ingredient.IngredientId;
            _isEditMode = true;
            InitializePage();

            // Загружаем данные для редактирования
            LoadIngredientData();
        }

        /// <summary>
        /// Инициализация страницы (заголовок и кнопка)
        /// </summary>
        private void InitializePage()
        {
            if (_isEditMode)
            {
                // Простой способ изменить заголовок (рекомендуется добавить x:Name="PageTitle" в XAML)
                var titleBlock = this.FindName("PageTitle") as TextBlock;
                if (titleBlock != null)
                {
                    titleBlock.Text = "Редактирование ингредиента";
                }

                // Меняем текст кнопки
                var buttonContent = CreateButton.Content as StackPanel;
                if (buttonContent != null)
                {
                    foreach (var child in buttonContent.Children)
                    {
                        if (child is TextBlock tb && tb.Text == "Создать ингредиент")
                        {
                            tb.Text = "Сохранить изменения";
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Загрузка данных ингредиента из БД
        /// </summary>
        private void LoadIngredientData()
        {
            try
            {
                using (var ctx = new TrackSyncContext())
                {
                    var ingredient = ctx.Ingredients
                        .FirstOrDefault(i => i.IngredientId == _editingIngredientId);

                    if (ingredient == null)
                    {
                        ErrorMessageVisible("*Ингредиент не найден");
                        return;
                    }

                    // Заполняем поля
                    TitleTextBox.Text = ingredient.Title;
                    CaloriesTextBox.Text = ingredient.Calories.ToString();
                    FatsTextBox.Text = ingredient.Fats.ToString();
                    ProteinsTextBox.Text = ingredient.Proteins.ToString();
                    CarbsTextBox.Text = ingredient.Carbs.ToString();
                    PriceTextBox.Text = ingredient.Price.ToString();
                    RecommendedWeightTextBox.Text = ingredient.RecommendedWeight ?? string.Empty;
                }
            }
            catch (Exception ex)
            {
                ErrorMessageVisible($"*Ошибка загрузки данных: {ex.Message}");
            }
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
            ErrorMessageHide();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (FrameContext.MainWindowFrame.CanGoBack)
                FrameContext.MainWindowFrame.GoBack();
        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorMessageHide();

            if (!ValidateFields())
                return;

            try
            {
                using (var ctx = new TrackSyncContext())
                {
                    if (_isEditMode)
                    {
                        // ===== РЕДАКТИРОВАНИЕ =====
                        var ingredientToUpdate = ctx.Ingredients
                            .FirstOrDefault(i => i.IngredientId == _editingIngredientId);

                        if (ingredientToUpdate == null)
                        {
                            ErrorMessageVisible("*Ингредиент для редактирования не найден");
                            return;
                        }

                        // Обновляем данные
                        ingredientToUpdate.Title = TitleTextBox.Text.Trim();
                        ingredientToUpdate.Calories = int.Parse(CaloriesTextBox.Text);
                        ingredientToUpdate.Fats = int.Parse(FatsTextBox.Text);
                        ingredientToUpdate.Proteins = int.Parse(ProteinsTextBox.Text);
                        ingredientToUpdate.Carbs = int.Parse(CarbsTextBox.Text);
                        ingredientToUpdate.Price = int.Parse(PriceTextBox.Text);
                        ingredientToUpdate.RecommendedWeight = string.IsNullOrWhiteSpace(RecommendedWeightTextBox.Text)
                            ? null
                            : RecommendedWeightTextBox.Text.Trim();

                        ctx.SaveChanges();
                    }
                    else
                    {
                        // ===== СОЗДАНИЕ =====
                        var newIngredient = new Ingredient(
                            title: TitleTextBox.Text.Trim(),
                            calories: int.Parse(CaloriesTextBox.Text),
                            fats: int.Parse(FatsTextBox.Text),
                            proteins: int.Parse(ProteinsTextBox.Text),
                            carbs: int.Parse(CarbsTextBox.Text),
                            price: int.Parse(PriceTextBox.Text),
                            creatorUserId: SessionManager.CurrentUserId,
                            recommendedWeight: string.IsNullOrWhiteSpace(RecommendedWeightTextBox.Text)
                                ? null
                                : RecommendedWeightTextBox.Text.Trim()
                        );

                        ctx.Ingredients.Add(newIngredient);
                        ctx.SaveChanges();
                    }

                    // Возврат на предыдущую страницу
                    if (FrameContext.MainWindowFrame.CanGoBack)
                        FrameContext.MainWindowFrame.GoBack();
                }
            }
            catch (Exception ex)
            {
                ErrorMessageVisible($"*Ошибка при сохранении: {ex.Message}");
            }
        }

        private bool ValidateFields()
        {
            // Проверка названия
            if (string.IsNullOrWhiteSpace(TitleTextBox?.Text))
            {
                ErrorMessageVisible("*Название ингредиента не может быть пустым");
                return false;
            }

            if (TitleTextBox.Text.Length > 256)
            {
                ErrorMessageVisible("*Название не должно превышать 256 символов");
                return false;
            }

            // Проверка калорийности
            if (string.IsNullOrWhiteSpace(CaloriesTextBox?.Text))
            {
                ErrorMessageVisible("*Калорийность не может быть пустой");
                return false;
            }

            if (!int.TryParse(CaloriesTextBox.Text, out int calories) || calories < 0)
            {
                ErrorMessageVisible("*Калорийность должна быть неотрицательным целым числом");
                return false;
            }

            // Проверка белков
            if (string.IsNullOrWhiteSpace(ProteinsTextBox?.Text))
            {
                ErrorMessageVisible("*Белки не могут быть пустыми");
                return false;
            }

            if (!int.TryParse(ProteinsTextBox.Text, out int proteins) || proteins < 0)
            {
                ErrorMessageVisible("*Белки должны быть неотрицательным целым числом");
                return false;
            }

            // Проверка жиров
            if (string.IsNullOrWhiteSpace(FatsTextBox?.Text))
            {
                ErrorMessageVisible("*Жиры не могут быть пустыми");
                return false;
            }

            if (!int.TryParse(FatsTextBox.Text, out int fats) || fats < 0)
            {
                ErrorMessageVisible("*Жиры должны быть неотрицательным целым числом");
                return false;
            }

            // Проверка углеводов
            if (string.IsNullOrWhiteSpace(CarbsTextBox?.Text))
            {
                ErrorMessageVisible("*Углеводы не могут быть пустыми");
                return false;
            }

            if (!int.TryParse(CarbsTextBox.Text, out int carbs) || carbs < 0)
            {
                ErrorMessageVisible("*Углеводы должны быть неотрицательным целым числом");
                return false;
            }

            // Проверка цены
            if (string.IsNullOrWhiteSpace(PriceTextBox?.Text))
            {
                ErrorMessageVisible("*Цена не может быть пустой");
                return false;
            }

            if (!int.TryParse(PriceTextBox.Text, out int price) || price < 0)
            {
                ErrorMessageVisible("*Цена должна быть неотрицательным целым числом");
                return false;
            }

            // Проверка рекомендованного веса (необязательное поле)
            if (!string.IsNullOrWhiteSpace(RecommendedWeightTextBox?.Text))
            {
                if (RecommendedWeightTextBox.Text.Length > 40)
                {
                    ErrorMessageVisible("*Рекомендованный вес не должен превышать 40 символов");
                    return false;
                }
            }

            return true;
        }
    }
}