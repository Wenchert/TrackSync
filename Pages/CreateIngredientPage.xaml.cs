using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TrackSync.DataBase;
//using TrackSync.DataBase.Model;
using TrackSync.DataBase.WorkWithDB;
using TrackSync.Windows;

namespace TrackSync.Pages
{
    public partial class CreateIngredientPage : Page
    {
        public CreateIngredientPage()
        {
            InitializeComponent();
        }

        // Методы для работы с сообщением об ошибке (как в авторизации)
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
            ErrorMessageHide(); // Скрываем ошибку перед валидацией

            if (!ValidateFields())
            {
                return;
            }

            var newIngredient = new Ingredient(
                title: TitleTextBox.Text.Trim(),
                calories: int.Parse(CaloriesTextBox.Text),
                fats: int.Parse(FatsTextBox.Text),
                proteins: int.Parse(ProteinsTextBox.Text),
                carbs: int.Parse(CarbsTextBox.Text),
                price: int.Parse(PriceTextBox.Text),
                recommendedWeight: RecommendedWeightTextBox.Text
            );

            // Сохраняем в базу данных
            using (var ctx = new TrackSyncContext())
            {
                ctx.Ingredients.Add(newIngredient);
                ctx.SaveChanges();
            }

            // Возврат на предыдущую страницу (каталог ингредиентов)
            if (FrameContext.MainWindowFrame.CanGoBack)
            {
                FrameContext.MainWindowFrame.GoBack();
            }

        }

        private bool ValidateFields()
        {
            // Проверка названия
            if (string.IsNullOrWhiteSpace(TitleTextBox.Text))
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
            if (string.IsNullOrWhiteSpace(CaloriesTextBox.Text))
            {
                ErrorMessageVisible("*Калорийность не может быть пустой");
                return false;
            }

            if (!int.TryParse(CaloriesTextBox.Text, out int calories))
            {
                ErrorMessageVisible("*Калорийность должна быть целым числом");
                return false;
            }

            if (calories < 0)
            {
                ErrorMessageVisible("*Калорийность не может быть отрицательной");
                return false;
            }

            // Проверка белков
            if (string.IsNullOrWhiteSpace(ProteinsTextBox.Text))
            {
                ErrorMessageVisible("*Белки не могут быть пустыми");
                return false;
            }

            if (!int.TryParse(ProteinsTextBox.Text, out int proteins))
            {
                ErrorMessageVisible("*Белки должны быть целым числом");
                return false;
            }

            if (proteins < 0)
            {
                ErrorMessageVisible("*Белки не могут быть отрицательными");
                return false;
            }

            // Проверка жиров
            if (string.IsNullOrWhiteSpace(FatsTextBox.Text))
            {
                ErrorMessageVisible("*Жиры не могут быть пустыми");
                return false;
            }

            if (!int.TryParse(FatsTextBox.Text, out int fats))
            {
                ErrorMessageVisible("*Жиры должны быть целым числом");
                return false;
            }

            if (fats < 0)
            {
                ErrorMessageVisible("*Жиры не могут быть отрицательными");
                return false;
            }

            // Проверка углеводов
            if (string.IsNullOrWhiteSpace(CarbsTextBox.Text))
            {
                ErrorMessageVisible("*Углеводы не могут быть пустыми");
                return false;
            }

            if (!int.TryParse(CarbsTextBox.Text, out int carbs))
            {
                ErrorMessageVisible("*Углеводы должны быть целым числом");
                return false;
            }

            if (carbs < 0)
            {
                ErrorMessageVisible("*Углеводы не могут быть отрицательными");
                return false;
            }

            // Проверка цены
            if (string.IsNullOrWhiteSpace(PriceTextBox.Text))
            {
                ErrorMessageVisible("*Цена не может быть пустой");
                return false;
            }

            if (!int.TryParse(PriceTextBox.Text, out int price))
            {
                ErrorMessageVisible("*Цена должна быть целым числом");
                return false;
            }

            if (price < 0)
            {
                ErrorMessageVisible("*Цена не может быть отрицательной");
                return false;
            }

            // Проверка рекомендованного веса (необязательное поле)
            if (!string.IsNullOrWhiteSpace(RecommendedWeightTextBox.Text))
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