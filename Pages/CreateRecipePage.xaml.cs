using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TrackSync.DataBase;
using TrackSync.DataBase.Model;
using TrackSync.DataBase.WorkWithDB;
using TrackSync.Functions;
using TrackSync.Interface;
using TrackSync.Windows;

namespace TrackSync.Pages
{
    public partial class CreateRecipePage : Page, IRecipeIngredientCallback
    {
        private readonly int? _editingRecipeId; // ID редактируемого рецепта
        private readonly bool _isEditMode; // Флаг режима редактирования
        private readonly List<IngredientQuantityItem> _selectedIngredients = new();

        /// <summary>
        /// Конструктор для создания НОВОГО рецепта
        /// </summary>
        public CreateRecipePage()
        {
            InitializeComponent();
            _isEditMode = false;
            InitializePage();
        }

        /// <summary>
        /// Конструктор для РЕДАКТИРОВАНИЯ существующего рецепта
        /// </summary>
        public CreateRecipePage(Recipe recipe)
        {
            InitializeComponent();
            _editingRecipeId = recipe.RecipeId;
            _isEditMode = true;
            InitializePage();

            // Загружаем данные для редактирования
            LoadRecipeData();
        }

        /// <summary>
        /// Инициализация страницы (заголовок и кнопка)
        /// </summary>
        private void InitializePage()
        {
            if (_isEditMode)
            {
                // Изменяем заголовок страницы
                var titleBlock = this.FindName("PageTitle") as TextBlock;
                if (titleBlock != null)
                {
                    titleBlock.Text = "Редактирование рецепта";
                }

                // Меняем текст кнопки
                var createButtonContent = CreateButton.Content as TextBlock;
                if (createButtonContent != null)
                {
                    createButtonContent.Text = "Сохранить изменения";
                }
            }
        }

        /// <summary>
        /// Загрузка данных рецепта из БД с ингредиентами
        /// </summary>
        private void LoadRecipeData()
        {
            try
            {
                using (var ctx = new TrackSyncContext())
                {
                    var recipe = ctx.Recipes
                        .Include(r => r.IngredientsRecipes)
                        .ThenInclude(ir => ir.Ingredient)
                        .FirstOrDefault(r => r.RecipeId == _editingRecipeId);

                    if (recipe == null)
                    {
                        ErrorMessageVisible("*Рецепт не найден");
                        return;
                    }

                    // Заполняем основные поля
                    TitleTextBox.Text = recipe.Title;
                    TimeTextBox.Text = recipe.Time.ToString();
                    DescriptionTextBox.Text = recipe.Description ?? string.Empty;

                    // Загружаем ингредиенты
                    _selectedIngredients.Clear();
                    foreach (var ir in recipe.IngredientsRecipes)
                    {
                        if (ir.Ingredient != null)
                        {
                            _selectedIngredients.Add(new IngredientQuantityItem
                            {
                                Ingredient = ir.Ingredient,
                                Quantity = ir.Weight,
                                OriginalRecipeIngredientId = ir.IngredientId
                            });
                        }
                    }
                    // Обновляем интерфейс
                    UpdateIngredientsUI();
                }
            }
            catch (Exception ex)
            {
                ErrorMessageVisible($"*Ошибка загрузки данных: {ex.Message}");
            }
        }

        // Реализация интерфейса для получения выбранных ингредиентов
        public void OnIngredientsSelected(List<Ingredient> ingredients)
        {
            if (ingredients == null || ingredients.Count == 0)
                return;

            foreach (var ingredient in ingredients)
            {
                if (!_selectedIngredients.Exists(i => i.Ingredient.IngredientId == ingredient.IngredientId))
                {
                    _selectedIngredients.Add(new IngredientQuantityItem
                    {
                        Ingredient = ingredient,
                        Quantity = 100
                    });
                }
            }

            UpdateIngredientsUI();
            ErrorMessageHide();
        }

        private void UpdateIngredientsUI()
        {
            IngredientsStackPanel.Children.Clear();

            if (_selectedIngredients.Count == 0)
            {
                EmptyIngredientsText.Visibility = Visibility.Visible;
                return;
            }

            EmptyIngredientsText.Visibility = Visibility.Collapsed;

            foreach (var item in _selectedIngredients)
            {
                var ingredientPanel = CreateIngredientPanel(item);
                IngredientsStackPanel.Children.Add(ingredientPanel);
            }
        }

        private StackPanel CreateIngredientPanel(IngredientQuantityItem item)
        {
            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 10),
                Tag = item
            };

            var nameText = new TextBlock
            {
                Text = item.Ingredient.Title,
                Style = (Style)FindResource("BodyText"),
                VerticalAlignment = VerticalAlignment.Center,
                Width = 250,
                TextWrapping = TextWrapping.Wrap
            };
            panel.Children.Add(nameText);

            var quantityTextBox = new TextBox
            {
                Width = 100,
                Height = 36,
                Margin = new Thickness(10, 0, 0, 0),
                Text = item.Quantity.ToString(),
                Tag = item,
                Style = (Style)FindResource("InputField")
            };
            quantityTextBox.TextChanged += QuantityTextBox_TextChanged;
            quantityTextBox.PreviewTextInput += QuantityTextBox_PreviewTextInput;
            panel.Children.Add(quantityTextBox);

            var gramText = new TextBlock
            {
                Text = "грамм",
                Style = (Style)FindResource("BodyText"),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(5, 0, 0, 0),
                Foreground = (Brush)new BrushConverter().ConvertFrom("#7F8C8D")
            };
            panel.Children.Add(gramText);

            var removeButton = new Button
            {
                Width = 36,
                Height = 36,
                Style = (Style)FindResource("IconButton"),
                Margin = new Thickness(10, 0, 0, 0),
                Tag = panel,
                ToolTip = "Удалить ингредиент"
            };

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

        private void QuantityTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox && textBox.Tag is IngredientQuantityItem item)
            {
                // Если поле пустое, устанавливаем 0
                if (string.IsNullOrWhiteSpace(textBox.Text))
                {
                    item.Quantity = 0;
                    return;  // Выходим раньше, не заменяем текст
                }

                if (int.TryParse(textBox.Text, out int quantity) && quantity >= 0)  // Разрешаем 0
                {
                    item.Quantity = quantity;
                }
                    ErrorMessageHide();
            }
        }

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
            ErrorMessageHide();
        }

        private void RemoveIngredientButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is StackPanel panel && panel.Tag is IngredientQuantityItem item)
            {
                _selectedIngredients.Remove(item);
                UpdateIngredientsUI();
                ErrorMessageHide();
            }
        }

        private void SelectFromCatalogButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorMessageHide();
            FrameContext.MainWindowFrame.Navigate(new CatalogIngredientsPage(this));
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (FrameContext.MainWindowFrame.CanGoBack)
                FrameContext.MainWindowFrame.GoBack();
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
                        // ===== РЕДАКТИРОВАНИЕ =====
                        var recipeToUpdate = ctx.Recipes
                            .Include(r => r.IngredientsRecipes)
                            .FirstOrDefault(r => r.RecipeId == _editingRecipeId);

                        if (recipeToUpdate == null)
                        {
                            ErrorMessageVisible("*Рецепт для редактирования не найден");
                            return;
                        }

                        // Обновляем основные поля
                        recipeToUpdate.Title = TitleTextBox.Text.Trim();
                        recipeToUpdate.Time = int.Parse(TimeTextBox.Text);
                        recipeToUpdate.Description = string.IsNullOrWhiteSpace(DescriptionTextBox.Text)
                            ? null
                            : DescriptionTextBox.Text.Trim();

                        // Удаляем старые связи ингредиентов
                        var existingLinks = ctx.IngredientsRecipes
                            .Where(ir => ir.RecipeId == _editingRecipeId)
                            .ToList();

                        ctx.IngredientsRecipes.RemoveRange(existingLinks);

                        // Добавляем новые связи
                        foreach (var item in _selectedIngredients)
                        {
                            ctx.IngredientsRecipes.Add(new IngredientsRecipe
                            {
                                RecipeId = _editingRecipeId.Value,
                                IngredientId = item.Ingredient.IngredientId,
                                Weight = item.Quantity
                            });
                        }

                        ctx.SaveChanges();
                    }
                    else
                    {
                        // ===== СОЗДАНИЕ =====
                        var newRecipe = new Recipe
                        {
                            Title = TitleTextBox.Text.Trim(),
                            Time = int.Parse(TimeTextBox.Text),
                            Description = string.IsNullOrWhiteSpace(DescriptionTextBox.Text)
                                ? null
                                : DescriptionTextBox.Text.Trim(),
                            CreatorUserId = SessionManager.CurrentUserId 
                        };

                        ctx.Recipes.Add(newRecipe);
                        ctx.SaveChanges();

                        // Сохраняем ингредиенты
                        foreach (var item in _selectedIngredients)
                        {
                            ctx.IngredientsRecipes.Add(new IngredientsRecipe
                            {
                                RecipeId = newRecipe.RecipeId,
                                IngredientId = item.Ingredient.IngredientId,
                                Weight = item.Quantity
                            });
                        }

                        ctx.SaveChanges();
                    }

                    if (FrameContext.MainWindowFrame.CanGoBack)
                        FrameContext.MainWindowFrame.GoBack();
                }
            }
            catch (Exception ex)
            {
                ErrorMessageVisible($"*Ошибка при сохранении: {ex.Message}");
            }
        }

        private bool ValidateForm()
        {
            if (string.IsNullOrWhiteSpace(TitleTextBox?.Text))
            {
                ErrorMessageVisible("*Пожалуйста, введите название рецепта");
                return false;
            }

            if (TitleTextBox.Text.Length > 50)
            {
                ErrorMessageVisible("*Название не должно превышать 50 символов");
                return false;
            }

            if (string.IsNullOrWhiteSpace(TimeTextBox?.Text))
            {
                ErrorMessageVisible("*Пожалуйста, введите корректное время готовки (положительное число)");
                return false;
            }

            if (!int.TryParse(TimeTextBox.Text, out int time) || time <= 0) 
            {
                ErrorMessageVisible("*Слишком большое число для времени");
                return false;
            }

            if (_selectedIngredients.Count == 0)
            {
                ErrorMessageVisible("*Пожалуйста, добавьте хотя бы один ингредиент");
                return false;
            }

            foreach (var item in _selectedIngredients)
            {
                if (item.Quantity <= 0)
                {
                    ErrorMessageVisible($"*Пожалуйста, введите корректное количество для ингредиента \"{item.Ingredient.Title}\"");
                    return false;
                }

                if (item.Quantity >= int.MaxValue)
                {
                    ErrorMessageVisible($"*Слишком большое количество ингредиента \"{item.Ingredient.Title}\"");
                    return false;
                }
            }

            return true;
        }

        private void ErrorMessageVisible(string message)
        {
            ErrorMessage.Visibility = Visibility.Visible;
            ErrorMessage.Text = message;
        }

        private void ErrorMessageHide()
        {
            ErrorMessage.Visibility = Visibility.Collapsed;
        }

        // Улучшенный вспомогательный класс с поддержкой ID связи для редактирования
        private class IngredientQuantityItem
        {
            public Ingredient Ingredient { get; set; }
            public int Quantity { get; set; }
            public int? OriginalRecipeIngredientId { get; set; } // ID существующей связи (для редактирования)
        }
    }
}