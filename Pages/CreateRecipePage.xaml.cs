using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TrackSync.DataBase;
//using TrackSync.DataBase.Model;
using TrackSync.DataBase.WorkWithDB;
using TrackSync.Interface;
using TrackSync.Windows;

namespace TrackSync.Pages
{
    public partial class CreateRecipePage : Page, IRecipeIngredientCallback
    {
        // Список для хранения выбранных ингредиентов с количеством
        private readonly List<IngredientQuantityItem> _selectedIngredients = new();

        public CreateRecipePage()
        {
            InitializeComponent();
        }

        // Реализация интерфейса для получения выбранных ингредиентов
        public void OnIngredientsSelected(List<Ingredient> ingredients)
        {
            if (ingredients == null || ingredients.Count == 0)
                return;

            // Добавляем новые ингредиенты в список (без дубликатов)
            foreach (var ingredient in ingredients)
            {
                // Проверяем, не добавлен ли уже этот ингредиент
                if (!_selectedIngredients.Exists(i => i.Ingredient.IngredientId == ingredient.IngredientId))
                {
                    _selectedIngredients.Add(new IngredientQuantityItem
                    {
                        Ingredient = ingredient,
                        Quantity = 100 // Значение по умолчанию
                    });
                }
            }

            // Обновляем отображение
            UpdateIngredientsUI();
            ErrorMessageHide(); // Скрываем ошибку после добавления
        }

        // Обновление интерфейса списка ингредиентов
        private void UpdateIngredientsUI()
        {
            // Очищаем текущий список
            IngredientsStackPanel.Children.Clear();

            if (_selectedIngredients.Count == 0)
            {
                // Показываем сообщение "Список пуст"
                EmptyIngredientsText.Visibility = Visibility.Visible;
                return;
            }

            EmptyIngredientsText.Visibility = Visibility.Collapsed;

            // Добавляем каждый ингредиент в интерфейс
            foreach (var item in _selectedIngredients)
            {
                var ingredientPanel = CreateIngredientPanel(item);
                IngredientsStackPanel.Children.Add(ingredientPanel);
            }
        }

        // Создание панели для отображения ингредиента
        private StackPanel CreateIngredientPanel(IngredientQuantityItem item)
        {
            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 10),
                Tag = item // Сохраняем ссылку на объект для последующего обновления
            };

            // Название ингредиента
            var nameText = new TextBlock
            {
                Text = item.Ingredient.Title,
                Style = (Style)FindResource("BodyText"),
                VerticalAlignment = VerticalAlignment.Center,
                Width = 250,
                TextWrapping = TextWrapping.Wrap
            };
            panel.Children.Add(nameText);

            // Поле для ввода количества СО СТИЛЕМ InputField
            var quantityTextBox = new TextBox
            {
                Width = 100,
                Height = 36, // Высота как у других полей ввода
                Margin = new Thickness(10, 0, 0, 0),
                Text = item.Quantity.ToString(),
                Tag = item,
                Style = (Style)FindResource("InputField") // ← ПРИМЕНЯЕМ СТИЛЬ
            };
            quantityTextBox.TextChanged += QuantityTextBox_TextChanged;
            quantityTextBox.PreviewTextInput += QuantityTextBox_PreviewTextInput;
            panel.Children.Add(quantityTextBox);

            // Текст "грамм"
            var gramText = new TextBlock
            {
                Text = "грамм",
                Style = (Style)FindResource("BodyText"),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(5, 0, 0, 0),
                Foreground = (Brush)new BrushConverter().ConvertFrom("#7F8C8D")
            };
            panel.Children.Add(gramText);

            // Кнопка удаления С ИКОНКОЙ КОРЗИНЫ
            var removeButton = new Button
            {
                Width = 36,
                Height = 36,
                Style = (Style)FindResource("IconButton"), // Используем стиль иконок
                Margin = new Thickness(10, 0, 0, 0),
                Tag = panel,
                ToolTip = "Удалить ингредиент"
            };

            // Добавляем иконку корзины
            var image = new Image
            {
                Source = new BitmapImage(new Uri("/Images/Icons/trash.png", UriKind.Relative)),
                Width = 18,
                Height = 18
            };
            removeButton.Content = image;

            removeButton.Click += RemoveIngredientButton_Click;
            panel.Children.Add(removeButton);

            return panel;
        }

        // Обработчик изменения количества
        private void QuantityTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox && textBox.Tag is IngredientQuantityItem item)
            {
                if (int.TryParse(textBox.Text, out int quantity) && quantity > 0)
                {
                    item.Quantity = quantity;
                }
                else
                {
                    // Если введено некорректное значение, восстанавливаем предыдущее
                    textBox.Text = item.Quantity.ToString();
                }
                ErrorMessageHide(); // Скрываем ошибку при изменении
            }
        }

        // Валидация ввода (только цифры)
        private void QuantityTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            foreach (char c in e.Text)
            {
                if (!char.IsDigit(c))
                {
                    e.Handled = true;
                    break;
                }
            }
            ErrorMessageHide(); // Скрываем ошибку при вводе
        }

        // Удаление ингредиента
        private void RemoveIngredientButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is StackPanel panel && panel.Tag is IngredientQuantityItem item)
            {
                _selectedIngredients.Remove(item);
                UpdateIngredientsUI();
                ErrorMessageHide(); // Скрываем ошибку после удаления
            }
        }

        // Кнопка "Выбрать из каталога"
        private void SelectFromCatalogButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorMessageHide(); // Скрываем ошибку перед переходом
            FrameContext.MainWindowFrame.Navigate(new CatalogIngredientsPage(this));
        }

        // Кнопка "Отмена"
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (FrameContext.MainWindowFrame.CanGoBack)
                FrameContext.MainWindowFrame.GoBack();
        }

        // Кнопка "Создать рецепт"
        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorMessageHide(); // Скрываем ошибку перед валидацией

            // Валидация полей
            if (!ValidateForm())
                return;

            try
            {
                // Создаем новый рецепт
                var newRecipe = new Recipe
                {
                    Title = TitleTextBox.Text.Trim(),
                    Time = int.Parse(TimeTextBox.Text),
                    Description = string.IsNullOrWhiteSpace(DescriptionTextBox.Text)
                        ? null
                        : DescriptionTextBox.Text.Trim()
                };

                // Сохраняем рецепт в базу данных
                using (var ctx = new TrackSyncContext())
                {
                    ctx.Recipes.Add(newRecipe);
                    ctx.SaveChanges();

                    // Сохраняем ингредиенты рецепта
                    foreach (var item in _selectedIngredients)
                    {
                        var ingredientRecipe = new IngredientsRecipe
                        {
                            IngredientId = item.Ingredient.IngredientId,
                            RecipeId = newRecipe.RecipeId,
                            Weight = item.Quantity
                        };
                        ctx.IngredientsRecipes.Add(ingredientRecipe);
                    }

                    ctx.SaveChanges();
                }

                MessageBox.Show($"Рецепт \"{newRecipe.Title}\" успешно создан!",
                              "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                // Возвращаемся назад
                if (FrameContext.MainWindowFrame.CanGoBack)
                    FrameContext.MainWindowFrame.GoBack();
            }
            catch (Exception ex)
            {
                ErrorMessageVisible($"*Ошибка при создании рецепта: {ex.Message}");
            }
        }

        // Валидация формы (теперь с единым полем ошибок)
        private bool ValidateForm()
        {
            // Проверка названия
            if (string.IsNullOrWhiteSpace(TitleTextBox.Text))
            {
                ErrorMessageVisible("*Пожалуйста, введите название рецепта");
                return false;
            }

            // Проверка времени готовки
            if (string.IsNullOrWhiteSpace(TimeTextBox.Text) ||
                !int.TryParse(TimeTextBox.Text, out int time) || time <= 0)
            {
                ErrorMessageVisible("*Пожалуйста, введите корректное время готовки (положительное число)");
                return false;
            }

            // Проверка ингредиентов
            if (_selectedIngredients.Count == 0)
            {
                ErrorMessageVisible("*Пожалуйста, добавьте хотя бы один ингредиент");
                return false;
            }

            // Проверка, что для каждого ингредиента введено положительное количество
            foreach (var item in _selectedIngredients)
            {
                if (item.Quantity <= 0)
                {
                    ErrorMessageVisible($"*Пожалуйста, введите корректное количество для ингредиента \"{item.Ingredient.Title}\"");
                    return false;
                }
            }

            return true;
        }

        // Методы для работы с сообщением об ошибке (как в авторизации)
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

        // Вспомогательный класс для хранения ингредиента и количества
        private class IngredientQuantityItem
        {
            public Ingredient Ingredient { get; set; }
            public int Quantity { get; set; }
        }
    }
}