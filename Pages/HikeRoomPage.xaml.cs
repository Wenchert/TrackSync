using GMap.NET;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;
using System.Xml.Linq;
using TrackSync.DataBase;
using TrackSync.DataBase.WorkWithDB;
using TrackSync.Functions;
using TrackSync.Interfaces;
using TrackSync.Windows;

namespace TrackSync.Pages
{
    public partial class HikeRoomPage : Page, IEquipmentCallback, IRecipeCallback
    {
        private List<DayViewModel> _allDays = new List<DayViewModel>();
        private int _roomId;
        private int _currentDay = 1;
        private int _currentParticipantId = 1;
        private string _currentMealTypeTag = "";
        private readonly Dictionary<int, List<EquipmentQuantityItem>> _participantEquipment = new();
        private bool _isLeader = false;
        private RouteStatItem _editingRouteStat = null;
        private bool _isEditingRoute = false;
        private Guid _currentEditingSessionId = Guid.NewGuid();

        private bool _canEditMealPlan = false;
        private bool _canEditEquipment = false;
        private bool _canEditTrack = false;

        // Вспомогательный класс для хранения снаряжения с количеством
        private class EquipmentQuantityItem
        {
            public Equipment Equipment { get; set; }
            public int Quantity { get; set; }
        }

        public class DayViewModel
        {
            public int DayNumber { get; set; }
            public DateOnly Date { get; set; }
            public List<MealViewModel> Meals { get; set; } = new();

            public int TotalDayCalories => Meals.Sum(m => m.TotalCalories);
            public int TotalDayProteins => Meals.Sum(m => m.TotalProteins);
            public int TotalDayFats => Meals.Sum(m => m.TotalFats);
            public int TotalDayCarbs => Meals.Sum(m => m.TotalCarbs);
            public int TotalDayWeight => Meals.Sum(m => m.TotalWeight);

            public string TotalDayCaloriesText => $"{TotalDayCalories} ккал";
            public string TotalDayProteinsText => $"{TotalDayProteins} г";
            public string TotalDayFatsText => $"{TotalDayFats} г";
            public string TotalDayCarbsText => $"{TotalDayCarbs} г";
            public string TotalDayWeightText => $"{TotalDayWeight} г";
        }

        public class MealViewModel
        {
            public class IngredientDisplay
            {
                public string Title { get; set; }
                public int TotalWeight { get; set; } // Вес, умноженный на количество участников
            }

            public Visibility ShowDescriptionButtonVisibility
            {
                get
                {
                    if (Recipe != null && !string.IsNullOrWhiteSpace(RecipeDescription))
                        return Visibility.Visible;
                    return Visibility.Collapsed;
                }
            }

            public int TypeMealId { get; set; }
            public string MealName { get; set; }
            public string Tag { get; set; }
            public Recipe Recipe { get; set; }
            public int RoomId { get; set; }

            public List<RecipesParticipant> ResponsibleParticipants { get; set; } = new List<RecipesParticipant>();

            public List<string> ResponsibleUserNames =>
                ResponsibleParticipants?
                    .Where(rp => rp.Participant?.User != null)
                    .Select(rp => rp.Participant.User.Name)
                    .ToList() ?? new List<string>();

            // Существующие свойства для общего количества (на всех участников)
            public int TotalCalories => Recipe != null ? CalculateTotalNutrients().Calories : 0;
            public int TotalProteins => Recipe != null ? CalculateTotalNutrients().Proteins : 0;
            public int TotalFats => Recipe != null ? CalculateTotalNutrients().Fats : 0;
            public int TotalCarbs => Recipe != null ? CalculateTotalNutrients().Carbs : 0;
            public int TotalWeight => Recipe != null ? CalculateTotalWeight() : 0;
            public int WeightPerPerson => Recipe != null ? CalculateWeightPerPerson() : 0;

            // Новые свойства для одного человека
            public int CaloriesPerPerson => Recipe != null ? CalculatePerPersonNutrients().Calories : 0;
            public int ProteinsPerPerson => Recipe != null ? CalculatePerPersonNutrients().Proteins : 0;
            public int FatsPerPerson => Recipe != null ? CalculatePerPersonNutrients().Fats : 0;
            public int CarbsPerPerson => Recipe != null ? CalculatePerPersonNutrients().Carbs : 0;

            // Форматированные текстовые свойства (для привязки в XAML)
            public string WeightPerPersonText => Recipe != null ?
                $"{TotalWeight}г ({WeightPerPerson}г/чел)" : string.Empty;

            public string CookTimeText => Recipe?.Time.HasValue == true ?
                $"~{Recipe.Time} мин." : "-";

            public string CaloriesText => Recipe != null ?
                $"{TotalCalories} ккал" : "0 ккал";

            public string ProteinsPerPersonText => Recipe != null ?
                $"{ProteinsPerPerson} г" : "0 г";

            public string FatsPerPersonText => Recipe != null ?
                $"{FatsPerPerson} г" : "0 г";

            public string CarbsPerPersonText => Recipe != null ?
                $"{CarbsPerPerson} г" : "0 г";

            // Рассчитываем нутриенты для одного человека
            private (int Calories, int Proteins, int Fats, int Carbs) CalculatePerPersonNutrients()
            {
                if (Recipe == null || Recipe.IngredientsRecipes == null)
                    return (0, 0, 0, 0);

                int calories = 0;
                int proteins = 0;
                int fats = 0;
                int carbs = 0;

                foreach (var ir in Recipe.IngredientsRecipes)
                {
                    if (ir.Ingredient != null)
                    {
                        // Расчет на одного человека (без умножения на количество участников)
                        calories += (ir.Ingredient.Calories * ir.Weight) / 100;
                        proteins += (ir.Ingredient.Proteins * ir.Weight) / 100;
                        fats += (ir.Ingredient.Fats * ir.Weight) / 100;
                        carbs += (ir.Ingredient.Carbs * ir.Weight) / 100;
                    }
                }

                return (calories, proteins, fats, carbs);
            }

            // Рассчитываем нутриенты для всех участников
            private (int Calories, int Proteins, int Fats, int Carbs) CalculateTotalNutrients()
            {
                if (Recipe == null || Recipe.IngredientsRecipes == null)
                    return (0, 0, 0, 0);

                int calories = 0;
                int proteins = 0;
                int fats = 0;
                int carbs = 0;

                int participantCount = GetParticipantCount(RoomId);

                foreach (var ir in Recipe.IngredientsRecipes)
                {
                    if (ir.Ingredient != null)
                    {
                        // Расчет на всех участников
                        calories += (ir.Ingredient.Calories * ir.Weight * participantCount) / 100;
                        proteins += (ir.Ingredient.Proteins * ir.Weight * participantCount) / 100;
                        fats += (ir.Ingredient.Fats * ir.Weight * participantCount) / 100;
                        carbs += (ir.Ingredient.Carbs * ir.Weight * participantCount) / 100;
                    }
                }

                return (calories, proteins, fats, carbs);
            }

            private int CalculateTotalWeight()
            {
                if (Recipe == null || Recipe.IngredientsRecipes == null)
                    return 0;

                int totalWeightPerPerson = Recipe.IngredientsRecipes.Sum(ir => ir.Weight);
                int participantCount = GetParticipantCount(RoomId);
                return totalWeightPerPerson * participantCount;
            }

            private int CalculateWeightPerPerson()
            {
                if (Recipe == null || Recipe.IngredientsRecipes == null)
                    return 0;

                return Recipe.IngredientsRecipes.Sum(ir => ir.Weight);
            }

            private static int GetParticipantCount(int roomId)
            {
                using var ctx = new TrackSyncContext();
                return ctx.Participants.Count(p => p.HikeRoomId == roomId);
            }

            public List<IngredientDisplay> IngredientsDisplay
            {
                get
                {
                    if (Recipe?.IngredientsRecipes == null)
                        return new List<IngredientDisplay>();

                    int participantCount = GetParticipantCount(RoomId);
                    return Recipe.IngredientsRecipes.Select(ir => new IngredientDisplay
                    {
                        Title = ir.Ingredient?.Title ?? "Неизвестный ингредиент",
                        TotalWeight = ir.Weight * participantCount
                    }).ToList();
                }
            }

            // Также добавьте свойство для отображения описания
            public string RecipeDescription => Recipe?.Description;
        }

        public class RouteStatItem
        {
            public string CreatorName { get; set; }
            public string ColorHex { get; set; }
            public Brush ColorBrush => (Brush)new BrushConverter().ConvertFromString(ColorHex ?? "#000000");
            public double Distance { get; set; }
            public string DistanceStr => $"{Distance:F2} км";
            public int PointsCount { get; set; }
            public int ParticipantId { get; set; }
            public Guid EditingSessionId { get; set; } = Guid.Empty;
        }

        public class IngredientStatItem
        {
            public string IngredientTitle { get; set; }
            public int IngredientId { get; set; }
            public int TotalWeight { get; set; }
            public decimal TotalCost { get; set; }
            public int UsedInRecipesCount { get; set; }
            public decimal CostPerPerson { get; set; }

            public string TotalWeightFormatted => $"{TotalWeight} г";
        }

        public HikeRoomPage(int roomId = 1)
        {
            InitializeComponent();
            _roomId = roomId;

            LoadRoomData();
            LoadMealPlan();
            RoomTabControl.SelectionChanged += RoomTabControl_SelectionChanged;

            this.Loaded += (s, e) =>
            {
                if (MyRouteBuilder != null)
                {
                    MyRouteBuilder.RouteUpdated += MyRouteBuilder_RouteUpdated;
                    MyRouteBuilder.ColorChanged += MyRouteBuilder_ColorChanged;
                }
            };
        }

        // Загрузка данных комнаты
        private void LoadRoomData()
        {
            var room = TrackSyncManagerList.GetHikeRoomById(_roomId);
            if (room == null)
            {
                MessageBox.Show("Комната не найдена!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                if (NavigationService?.CanGoBack == true) NavigationService.GoBack();
                return;
            }

            _isLeader = room.LeaderId == SessionManager.CurrentUserId;
            InviteUsersButton.Visibility = _isLeader ? Visibility.Visible : Visibility.Collapsed;
            RoomSettingsButton.Visibility = _isLeader ? Visibility.Visible : Visibility.Collapsed;

            RoomTitleText.Text = room.Title;
            string dateRange = $"{room.StartDate:dd MMMM yyyy} - {room.EndDate:dd MMMM yyyy}";
            var dateText = FindName("RoomDateText") as TextBlock;
            dateText.Text = dateRange;

            int participantCount = room.Participants?.Count ?? 0;
            var countText = FindName("ParticipantCountText") as TextBlock;
            countText.Text = $"Количество участников: {participantCount}";

            LoadParticipants();
            LoadMealPlan();

            // Сначала загружаем карту, потом обновляем
            RefreshMap();

            Dispatcher.BeginInvoke(() =>
            {
                ApplyPermissions();
            }, System.Windows.Threading.DispatcherPriority.Render);
        }
        private (bool canEditMealPlan, bool canEditEquipment, bool canEditTrack) GetCurrentUserPermissions()
        {
            using var ctx = new TrackSyncContext();
            var participant = ctx.Participants
                .FirstOrDefault(p => p.UserId == SessionManager.CurrentUserId && p.HikeRoomId == _roomId);

            return (
                canEditMealPlan: participant?.EditMealPlan ?? false,
                canEditEquipment: participant?.EditEquipment ?? false,
                canEditTrack: participant?.EditTrack ?? false
            );
        }

        private void ApplyPermissions()
        {
            var (canEditMealPlan, canEditEquipment, canEditTrack) = GetCurrentUserPermissions();

            // Сохраняем права в полях класса для использования в других методах
            _canEditMealPlan = canEditMealPlan;
            _canEditEquipment = canEditEquipment;
            _canEditTrack = canEditTrack;

            // 1. Кнопки управления маршрутом
            SaveRouteButton.Visibility = canEditTrack ? Visibility.Visible : Visibility.Collapsed;
            UndoRouteButton.Visibility = canEditTrack ? Visibility.Visible : Visibility.Collapsed;
            ClearRouteButton.Visibility = canEditTrack ? Visibility.Visible : Visibility.Collapsed;
            MyRouteBuilder.SetPermissions(canEditTrack);

            // 2. Кнопка "Сохранить изменения" снаряжения
            SaveChangesButton.Visibility = canEditEquipment ? Visibility.Visible : Visibility.Collapsed;

            // 3. Кнопки "Добавить снаряжение" для каждого участника
            if (ParticipantsItemsControl.IsLoaded)
            {
                foreach (var item in ParticipantsItemsControl.Items)
                {
                    var container = ParticipantsItemsControl.ItemContainerGenerator.ContainerFromItem(item);
                    if (container == null) continue;

                    var addButton = FindVisualChild<Button>(container,
                        btn => btn.Tag != null && int.TryParse(btn.Tag.ToString(), out _));

                    if (addButton != null)
                    {
                        addButton.Visibility = canEditEquipment ? Visibility.Visible : Visibility.Collapsed;
                    }
                }
            }

            // 4. Обновляем права для снаряжения
            UpdateEquipmentPermissions(canEditEquipment);

            // 5. Обновляем права для раскладки
            UpdateMealPlanPermissions(canEditMealPlan);
        }

        // Загрузка продуктовой раскладки
        private void LoadMealPlan()
        {
            using var ctx = new TrackSyncContext();
            var room = ctx.HikeRooms.Find(_roomId);

            if (room == null)
            {
                MessageBox.Show("Комната не найдена", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Правильно вычисляем количество дней
            int totalDays = (room.EndDate.ToDateTime(TimeOnly.MinValue) -
                           room.StartDate.ToDateTime(TimeOnly.MinValue)).Days + 1;

            var mealPlanData = TrackSyncManagerList.GetMealPlanForRoom(_roomId);

            if (mealPlanData == null || !mealPlanData.Any())
            {
                // Создаем базовую структуру
                _allDays = new List<DayViewModel>();

                for (int i = 0; i < totalDays; i++)
                {
                    var date = room.StartDate.AddDays(i);
                    var day = new DayViewModel
                    {
                        DayNumber = i + 1,
                        Date = date,
                        Meals = new List<MealViewModel>
                {
                    new MealViewModel {
                        TypeMealId = 1,
                        MealName = "Завтрак",
                        Tag = $"breakfast_day{i + 1}",
                        RoomId = _roomId
                    },
                    new MealViewModel {
                        TypeMealId = 2,
                        MealName = "Обед",
                        Tag = $"lunch_day{i + 1}",
                        RoomId = _roomId
                    },
                    new MealViewModel {
                        TypeMealId = 4,
                        MealName = "Перекус",
                        Tag = $"snack_day{i + 1}",
                        RoomId = _roomId
                    },
                    new MealViewModel {
                        TypeMealId = 3,
                        MealName = "Ужин",
                        Tag = $"dinner_day{i + 1}",
                        RoomId = _roomId
                    }
                }
                    };
                    _allDays.Add(day);
                }
            }
            else
            {
                // Преобразуем данные в DayViewModel
                _allDays = new List<DayViewModel>();

                foreach (var dayData in mealPlanData)
                {
                    var day = new DayViewModel
                    {
                        DayNumber = dayData.DayNumber,
                        Date = dayData.Date,
                        Meals = new List<MealViewModel>()
                    };

                    foreach (var mealData in dayData.Meals)
                    {
                        string mealName = mealData.TypeMealId switch
                        {
                            1 => "Завтрак",
                            2 => "Обед",
                            3 => "Ужин",
                            4 => "Перекус",
                            _ => "Неизвестно"
                        };

                        string englishMealName = mealData.TypeMealId switch
                        {
                            1 => "breakfast",
                            2 => "lunch",
                            3 => "dinner",
                            4 => "snack",
                            _ => "unknown"
                        };

                        string tag = $"{englishMealName}_day{dayData.DayNumber}";

                        var meal = new MealViewModel
                        {
                            TypeMealId = mealData.TypeMealId,
                            MealName = mealName,
                            Tag = tag,
                            Recipe = mealData.Recipe,
                            RoomId = _roomId,
                            ResponsibleParticipants = mealData.ResponsibleParticipants ?? new List<RecipesParticipant>()
                        };
                        day.Meals.Add(meal);
                    }

                    _allDays.Add(day);
                }

                // Проверяем, что все дни из диапазона присутствуют
                if (_allDays.Count < totalDays)
                {
                    // Дополняем недостающие дни
                    for (int dayNum = 1; dayNum <= totalDays; dayNum++)
                    {
                        if (!_allDays.Any(d => d.DayNumber == dayNum))
                        {
                            var date = room.StartDate.AddDays(dayNum - 1);
                            var newDay = new DayViewModel
                            {
                                DayNumber = dayNum,
                                Date = date,
                                Meals = new List<MealViewModel>
                        {
                            new MealViewModel {
                                TypeMealId = 1,
                                MealName = "Завтрак",
                                Tag = $"breakfast_day{dayNum}",
                                RoomId = _roomId
                            },
                            new MealViewModel {
                                TypeMealId = 2,
                                MealName = "Обед",
                                Tag = $"lunch_day{dayNum}",
                                RoomId = _roomId
                            },
                            new MealViewModel {
                                TypeMealId = 4,
                                MealName = "Перекус",
                                Tag = $"snack_day{dayNum}",
                                RoomId = _roomId
                            },
                            new MealViewModel {
                                TypeMealId = 3,
                                MealName = "Ужин",
                                Tag = $"dinner_day{dayNum}",
                                RoomId = _roomId
                            }
                        }
                            };
                            _allDays.Add(newDay);
                        }
                    }

                    // Сортируем дни по номеру
                    _allDays = _allDays.OrderBy(d => d.DayNumber).ToList();
                }

                Dispatcher.BeginInvoke(() =>
                {
                    ApplyPermissions();
                }, System.Windows.Threading.DispatcherPriority.Render);

            }

            // Кнопки дней: показываем ВСЕ дни
            DaysButtonsControl.ItemsSource = null; // Сначала очищаем
            DaysButtonsControl.ItemsSource = _allDays;

            Dispatcher.BeginInvoke(() =>
            {
                ApplyPermissions();
            }, System.Windows.Threading.DispatcherPriority.Render);

            // Контент: показываем первый день
            ShowCurrentDay(1);

            // Принудительно обновляем интерфейс
            Dispatcher.BeginInvoke(() =>
            {
                ActivateDayButton(1);
            }, System.Windows.Threading.DispatcherPriority.Render);
        }
        // Загрузка участников комнаты со снаряжением
        private void LoadParticipants()
        {
            var participants = TrackSyncManagerList.GetParticipantsWithEquipment(_roomId);

            if (participants != null && participants.Any())
            {
                // Очищаем словарь перед загрузкой новых данных
                _participantEquipment.Clear();

                // Загружаем снаряжение для каждого участника из БД
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

                    // Сохраняем список снаряжения для участника в словарь
                    _participantEquipment[participant.ParticipantId] = equipmentList;
                }

                // Устанавливаем источник данных для отображения участников
                ParticipantsItemsControl.ItemsSource = participants;

                // Подписываемся на событие загрузки контейнера для отложенного обновления интерфейса
                ParticipantsItemsControl.Loaded += (s, e) =>
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        // Дожидаемся полной загрузки ВСЕХ визуальных контейнеров
                        int attempts = 0;
                        var timer = new System.Windows.Threading.DispatcherTimer
                        {
                            Interval = TimeSpan.FromMilliseconds(50)
                        };

                        timer.Tick += (sender, args) =>
                        {
                            attempts++;

                            // Проверяем, созданы ли контейнеры для всех участников
                            bool allContainersReady = true;
                            foreach (var item in participants)
                            {
                                var container = ParticipantsItemsControl.ItemContainerGenerator.ContainerFromItem(item);
                                if (container == null || VisualTreeHelper.GetChildrenCount(container) == 0)
                                {
                                    allContainersReady = false;
                                    break;
                                }
                            }

                            // Если все контейнеры готовы или превышено время ожидания (1 секунда)
                            if (allContainersReady || attempts > 20)
                            {
                                timer.Stop();

                                // Обновляем интерфейс снаряжения для КАЖДОГО участника
                                foreach (var participant in participants)
                                {
                                    UpdateEquipmentUI(participant.ParticipantId);
                                }
                                ApplyPermissions();
                            }
                        };

                        timer.Start();
                    }), System.Windows.Threading.DispatcherPriority.Background);
                };
            }
        }
        // Загрузка ингредиентов для статистики
        private void LoadIngredientsStatistics()
        {
            try
            {
                using var ctx = new TrackSyncContext();

                // Получаем количество участников
                int participantsCount = ctx.Participants
                    .Count(p => p.HikeRoomId == _roomId);

                // Обновляем отображение количества участников
                if (ParticipantsCountRun != null)
                    ParticipantsCountRun.Text = participantsCount.ToString();

                // Получаем все рецепты, используемые в этой комнате
                var roomRecipes = ctx.HikeRoomsRecipes
                    .Include(hrr => hrr.Recipe)
                        .ThenInclude(r => r.IngredientsRecipes)
                            .ThenInclude(ir => ir.Ingredient)
                    .Where(hrr => hrr.HikeRoomId == _roomId)
                    .ToList();

                // Если нет рецептов, показываем сообщение
                if (!roomRecipes.Any())
                {
                    NoIngredientsText.Visibility = Visibility.Visible;
                    IngredientsStatisticsList.ItemsSource = null;
                    TotalCostText.Text = "0 руб.";
                    CostPerPersonText.Text = "0 руб.";
                    return;
                }

                NoIngredientsText.Visibility = Visibility.Collapsed;

                // Собираем статистику по ингредиентам
                var ingredientStats = new Dictionary<int, IngredientStatItem>();
                var recipeUsageCount = new Dictionary<int, int>();

                foreach (var roomRecipe in roomRecipes)
                {
                    var recipe = roomRecipe.Recipe;

                    if (recipe?.IngredientsRecipes == null)
                        continue;

                    foreach (var ingredientRecipe in recipe.IngredientsRecipes)
                    {
                        var ingredient = ingredientRecipe.Ingredient;
                        if (ingredient == null) continue;

                        // Рассчитываем общий вес для всех участников
                        // ingredientRecipe.Weight - вес на одного человека в граммах
                        int totalWeightForAllParticipants = ingredientRecipe.Weight * participantsCount;

                        // Рассчитываем стоимость
                        // Предполагаем, что Price в модели Ingredient - это цена за 100 грамм
                        decimal costPer100g = ingredient.Price;
                        decimal costForIngredient = (costPer100g * totalWeightForAllParticipants) / 100;

                        if (ingredientStats.ContainsKey(ingredient.IngredientId))
                        {
                            // Обновляем существующую запись
                            var existing = ingredientStats[ingredient.IngredientId];
                            existing.TotalWeight += totalWeightForAllParticipants;
                            existing.TotalCost += costForIngredient;
                            recipeUsageCount[ingredient.IngredientId]++;
                        }
                        else
                        {
                            // Создаем новую запись
                            ingredientStats[ingredient.IngredientId] = new IngredientStatItem
                            {
                                IngredientId = ingredient.IngredientId,
                                IngredientTitle = ingredient.Title,
                                TotalWeight = totalWeightForAllParticipants,
                                TotalCost = costForIngredient,
                                UsedInRecipesCount = 1
                            };
                            recipeUsageCount[ingredient.IngredientId] = 1;
                        }
                    }
                }

                // Обновляем счетчик использования в рецептах
                foreach (var stat in ingredientStats.Values)
                {
                    stat.UsedInRecipesCount = recipeUsageCount[stat.IngredientId];
                    // Рассчитываем стоимость на одного человека
                    stat.CostPerPerson = participantsCount > 0 ? stat.TotalCost / participantsCount : 0;
                }

                // Сортируем по стоимости (от большего к меньшему)
                var sortedStats = ingredientStats.Values
                    .OrderByDescending(i => i.TotalCost)
                    .ToList();

                // Устанавливаем источник данных
                IngredientsStatisticsList.ItemsSource = sortedStats;

                // Рассчитываем общие суммы
                decimal totalCost = sortedStats.Sum(i => i.TotalCost);
                decimal costPerPerson = participantsCount > 0 ? totalCost / participantsCount : 0;

                // Обновляем отображение
                TotalCostText.Text = $"{totalCost:F2} руб.";
                CostPerPersonText.Text = $"{costPerPerson:F2} руб.";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке статистики ингредиентов:\n{ex.Message}",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // === 1. Маршруты ===
        private void SaveRoute_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var points = MyRouteBuilder.RoutePoints;
                using (var db = new TrackSyncContext())
                {
                    using (var transaction = db.Database.BeginTransaction())
                    {
                        // Получаем участника
                        var participant = db.Participants.FirstOrDefault(p => p.UserId == SessionManager.CurrentUserId
                            && p.HikeRoomId == _roomId);

                        if (participant == null)
                        {
                            MessageBox.Show("Ошибка: участник не найден.");
                            return;
                        }

                        // Ищем или создаем трек
                        var myTrack = db.Tracks
                            .Include(t => t.Segments)
                            .FirstOrDefault(t => t.HikeRoomId == _roomId
                                && t.ParticipantId == participant.ParticipantId);

                        if (myTrack != null)
                        {
                            db.Segments.RemoveRange(myTrack.Segments);
                        }
                        else
                        {
                            myTrack = new Track
                            {
                                HikeRoomId = _roomId,
                                ParticipantId = participant.ParticipantId,
                                Title = $"Маршрут: {RoomTitleText.Text + " " + SessionManager.CurrentUser.Name}",
                            };
                            db.Tracks.Add(myTrack);
                        }

                        myTrack.Color = MyRouteBuilder.SelectedColor;
                        db.SaveChanges();

                        // Сохраняем точки и сегменты
                        Waypoint prevWaypoint = null;
                        for (int i = 0; i < points.Count; i++)
                        {
                            var currentWaypoint = new Waypoint
                            {
                                Latitude = (decimal)points[i].Lat,
                                Longitude = (decimal)points[i].Lng,
                                Title = $"Т{i + 1}",
                                Height = 0
                            };
                            db.Waypoints.Add(currentWaypoint);
                            db.SaveChanges();

                            if (prevWaypoint != null)
                            {
                                var dist = GMap.NET.MapProviders.EmptyProvider.Instance.Projection
                                    .GetDistance(points[i - 1], points[i]);
                                var segment = new Segment
                                {
                                    TrackId = myTrack.TrackId,
                                    StartWaypointId = prevWaypoint.WaypointId,
                                    EndWaypointId = currentWaypoint.WaypointId,
                                    Distance = (int)(dist * 1000)
                                };
                                db.Segments.Add(segment);
                            }
                            prevWaypoint = currentWaypoint;
                        }

                        db.SaveChanges();
                        transaction.Commit();

                        // После сохранения обновляем маршрут без центрирования
                        MyRouteBuilder.LoadRoute(points, MyRouteBuilder.SelectedColor, centerMap:false);
                        ShowMessage(MapMessage, "Маршрут сохранён", true);

                        // Сбрасываем состояние редактирования
                        _isEditingRoute = false;
                        _editingRouteStat = null;
                        _currentEditingSessionId = Guid.NewGuid();

                        // Обновляем карту и статистику
                        RefreshMap();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}");
            }
        }

        private void RefreshMap()
        {
            try
            {
                MyRouteBuilder.Clear();
                var allTracks = TrackSyncManagerList.GetAllTracksForRoom(_roomId);
                var usedColors = new List<string>();
                int myPartId = TrackSyncManagerList.GetCurrentParticipantId(SessionManager.CurrentUserId, _roomId);

                var allPoints = new List<GMap.NET.PointLatLng>();

                // Сначала собираем цвета занятые другими пользователями
                foreach (var track in allTracks)
                {
                    var points = ExtractPoints(track);
                    allPoints.AddRange(points);

                    if (track.ParticipantId != myPartId)
                    {
                        if (points.Count >= 2)
                        {
                            MyRouteBuilder.DisplayOtherTrack(points, track.Color);
                            usedColors.Add(track.Color);
                        }
                    }
                    else
                    {
                        if (points.Count >= 2 && !_isEditingRoute)
                        {
                            // Явно устанавливаем цвет из базы данных
                            string trackColor = !string.IsNullOrEmpty(track.Color) ? track.Color : "#FF0000";
                            MyRouteBuilder.LoadRoute(points, trackColor, centerMap: true);

                            // Обновляем выбранный цвет в панели выбора
                            MyRouteBuilder.SetColor(trackColor);
                        }
                    }
                }

                var hasMyTrack = allTracks.Any(t => t.ParticipantId == myPartId && ExtractPoints(t).Count >= 2);
                if (!_isEditingRoute && !hasMyTrack)
                {
                    string availableColor = MyRouteBuilder.GetFirstAvailableColor(usedColors);

                    if (availableColor == null && usedColors.Count > 0)
                    {
                        MyRouteBuilder.DisableEditing();
                        SaveRouteButton.Visibility = Visibility.Collapsed;
                        UndoRouteButton.Visibility = Visibility.Collapsed;
                        ClearRouteButton.Visibility = Visibility.Collapsed;
                        ShowMessage(MapMessage, "Все цвета маршрутов заняты. Невозможно создать новый маршрут.", false);
                    }
                    else if (availableColor != null)
                    {
                        MyRouteBuilder.SetColor(availableColor);
                    }
                }

                // Отключаем использованные цвета в палитре
                MyRouteBuilder.DisableUsedColors(usedColors);

                // Центрируем карту
                if (allPoints.Count > 0)
                {
                    MyRouteBuilder.CenterMapOnRoutes(allPoints);
                }
                else
                {
                    MyRouteBuilder.CenterMapOnPoint(new GMap.NET.PointLatLng(55.7558, 37.6173), 10);
                }

                // Обновляем статистику
                UpdateRoutesStatistics();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка обновления: {ex.Message}");
            }
        }

        private void MyRouteBuilder_RouteUpdated(object sender, (double distance, int points) e)
        {
            int myPartId = TrackSyncManagerList.GetCurrentParticipantId(SessionManager.CurrentUserId, _roomId);

            // Если есть точки
            if (e.points > 0)
            {
                if (!_isEditingRoute)
                {
                    _isEditingRoute = true;
                    _editingRouteStat = new RouteStatItem
                    {
                        CreatorName = SessionManager.CurrentUser?.Name ?? "Вы",
                        ColorHex = MyRouteBuilder.SelectedColor,
                        Distance = e.distance,
                        PointsCount = e.points,
                        ParticipantId = myPartId,
                        EditingSessionId = _currentEditingSessionId
                    };
                }
                else if (_editingRouteStat != null)
                {
                    _editingRouteStat.Distance = e.distance;
                    _editingRouteStat.PointsCount = e.points;
                    _editingRouteStat.ColorHex = MyRouteBuilder.SelectedColor;
                }
            }

            if (e.points > 0)
            {
                UpdateRoutesStatistics();
            }
        }

        private void MyRouteBuilder_ColorChanged(object sender, string newColor)
        {
            if (_isEditingRoute && _editingRouteStat != null)
            {
                _editingRouteStat.ColorHex = newColor;
                UpdateRoutesStatistics();
            }
        }

        // Динамическое обновление статистики
        private void UpdateRoutesStatistics()
        {
            try
            {
                var stats = new List<RouteStatItem>();
                int myPartId = TrackSyncManagerList.GetCurrentParticipantId(SessionManager.CurrentUserId, _roomId);

                // 1. Если есть редактируемый маршрут И ЕСТЬ ТОЧКИ - добавляем его
                if (_isEditingRoute && _editingRouteStat != null && _editingRouteStat.PointsCount > 0)
                {
                    stats.Add(_editingRouteStat);
                }

                // 2. Добавляем сохраненные маршруты других пользователей И текущего пользователя, если он НЕ в режиме редактирования
                var allTracks = TrackSyncManagerList.GetAllTracksForRoom(_roomId);

                foreach (var track in allTracks)
                {
                    // Если это текущий пользователь и он редактирует - пропускаем его сохраненный маршрут
                    if (track.ParticipantId == myPartId && _isEditingRoute)
                    {
                        continue;
                    }

                    var points = ExtractPoints(track);
                    if (points.Count >= 2)
                    {
                        double dist = 0;
                        for (int i = 0; i < points.Count - 1; i++)
                            dist += GMap.NET.MapProviders.EmptyProvider.Instance.Projection.GetDistance(points[i], points[i + 1]);

                        var statItem = new RouteStatItem
                        {
                            CreatorName = track.Participant?.User?.Name ?? "Участник",
                            ColorHex = track.Color ?? "#FF0000",
                            Distance = dist,
                            PointsCount = points.Count,
                            ParticipantId = track.ParticipantId
                        };

                        stats.Add(statItem);
                    }
                }

                // Сортируем по имени
                stats = stats.OrderBy(s => s.CreatorName).ToList();

                // Обновляем UI
                RoutesStatisticsList.ItemsSource = null;
                RoutesStatisticsList.ItemsSource = stats;
            }
            catch (Exception ex)
            {
                // В случае ошибки просто очищаем статистику
                RoutesStatisticsList.ItemsSource = null;
            }
        }

        //private List<GMap.NET.PointLatLng> ExtractPoints(Track track)
        //{
        //    var points = new List<GMap.NET.PointLatLng>();

        //    // Проверяем, что сегменты вообще есть
        //    if (track?.Segments == null || !track.Segments.Any()) return points;

        //    // Сортируем строго по ID, чтобы не было "паутины"
        //    var sortedSegments = track.Segments.OrderBy(s => s.SegmentId).ToList();

        //    // Берем начальную точку первого сегмента
        //    var first = sortedSegments.First();
        //    if (first.StartWaypoint != null)
        //    {
        //        points.Add(new GMap.NET.PointLatLng((double)first.StartWaypoint.Latitude, (double)first.StartWaypoint.Longitude));
        //    }

        //    // Добавляем конечные точки всех сегментов по порядку
        //    foreach (var s in sortedSegments)
        //    {
        //        if (s.EndWaypoint != null)
        //        {
        //            points.Add(new GMap.NET.PointLatLng((double)s.EndWaypoint.Latitude, (double)s.EndWaypoint.Longitude));
        //        }
        //    }

        //    return points;
        //}

        private void UndoRouteButton_Click(object sender, RoutedEventArgs e)
        {
            // Получаем текущее количество точек
            int pointsBefore = MyRouteBuilder.RoutePoints.Count;

            if (pointsBefore == 0)
            {
                return;
            }

            // Вызываем метод отмены
            MyRouteBuilder.UndoLastPoint();

            // Проверяем, стала ли количество точек равным 0
            // Делаем это с задержкой, чтобы событие RouteUpdated успело сработать
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (MyRouteBuilder.RoutePoints.Count == 0 && _isEditingRoute)
                {
                    // Получаем ID текущего участника
                    int myPartId = TrackSyncManagerList.GetCurrentParticipantId(SessionManager.CurrentUserId, _roomId);

                    // Сбрасываем флаги редактирования для ТЕКУЩЕГО ПОЛЬЗОВАТЕЛЯ
                    _isEditingRoute = false;
                    _editingRouteStat = null;
                    _currentEditingSessionId = Guid.NewGuid();

                    // ОБНОВЛЯЕМ СТАТИСТИКУ БЕЗ перезагрузки маршрутов из БД
                    var allTracks = TrackSyncManagerList.GetAllTracksForRoom(_roomId);
                    var stats = new List<RouteStatItem>();

                    foreach (var track in allTracks)
                    {
                        // Пропускаем маршруты текущего пользователя, так как мы их очистили
                        if (track.ParticipantId != myPartId)
                        {
                            var points = ExtractPoints(track);
                            if (points.Count >= 2)
                            {
                                double dist = 0;
                                for (int i = 0; i < points.Count - 1; i++)
                                    dist += GMap.NET.MapProviders.EmptyProvider.Instance.Projection.GetDistance(points[i], points[i + 1]);

                                var statItem = new RouteStatItem
                                {
                                    CreatorName = track.Participant?.User?.Name ?? "Участник",
                                    ColorHex = track.Color ?? "#FF0000",
                                    Distance = dist,
                                    PointsCount = points.Count,
                                    ParticipantId = track.ParticipantId
                                };

                                stats.Add(statItem);
                            }
                        }
                    }

                    // Сортируем и обновляем UI
                    stats = stats.OrderBy(s => s.CreatorName).ToList();
                    RoutesStatisticsList.ItemsSource = null;
                    RoutesStatisticsList.ItemsSource = stats;
                }
            }), System.Windows.Threading.DispatcherPriority.Background);
        }

        private void ClearRoute_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Получаем ID текущего участника
                int myPartId = TrackSyncManagerList.GetCurrentParticipantId(SessionManager.CurrentUserId, _roomId);

                // Очищаем только визуальную часть - точки на карте
                MyRouteBuilder.Clear();

                // Сбрасываем флаги редактирования для ТЕКУЩЕГО ПОЛЬЗОВАТЕЛЯ
                _isEditingRoute = false;
                _editingRouteStat = null;

                // Генерируем новый ID для следующей сессии редактирования
                _currentEditingSessionId = Guid.NewGuid();

                // ОБНОВЛЯЕМ СТАТИСТИКУ БЕЗ перезагрузки маршрутов из БД
                var allTracks = TrackSyncManagerList.GetAllTracksForRoom(_roomId);
                var stats = new List<RouteStatItem>();

                foreach (var track in allTracks)
                {
                    // Пропускаем маршруты текущего пользователя, так как мы их очистили
                    if (track.ParticipantId != myPartId)
                    {
                        var points = ExtractPoints(track);
                        if (points.Count >= 2)
                        {
                            double dist = 0;
                            for (int i = 0; i < points.Count - 1; i++)
                                dist += GMap.NET.MapProviders.EmptyProvider.Instance.Projection.GetDistance(points[i], points[i + 1]);

                            var statItem = new RouteStatItem
                            {
                                CreatorName = track.Participant?.User?.Name ?? "Участник",
                                ColorHex = track.Color ?? "#FF0000",
                                Distance = dist,
                                PointsCount = points.Count,
                                ParticipantId = track.ParticipantId
                            };

                            stats.Add(statItem);
                        }
                    }
                }

                // Сортируем и обновляем UI
                stats = stats.OrderBy(s => s.CreatorName).ToList();
                RoutesStatisticsList.ItemsSource = null;
                RoutesStatisticsList.ItemsSource = stats;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при очистке маршрута: {ex.Message}");
            }
        }

        // === 2. Снаряжение ===
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
            var equipmentStackPanel = FindEquipmentStackPanel(participantId);
            if (equipmentStackPanel == null)
                return;

            equipmentStackPanel.Children.Clear();

            if (_participantEquipment.TryGetValue(participantId, out var equipmentList) && equipmentList.Any())
            {
                foreach (var item in equipmentList)
                {
                    // Передаём participantId в метод создания панели
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
        // Создание панели для отображения снаряжения
        private StackPanel CreateEquipmentPanel(EquipmentQuantityItem item, int participantId)
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

            // Поле для ввода количества
            var quantityTextBox = new TextBox
            {
                Width = 80,
                Height = 36,
                Margin = new Thickness(10, 0, 0, 0),
                Text = item.Quantity.ToString(),
                Tag = item,
                Style = (Style)FindResource("InputField")
            };

            // Проверяем права для поля ввода
            using var ctx = new TrackSyncContext();
            var currentUser = ctx.Participants
                .FirstOrDefault(p => p.UserId == SessionManager.CurrentUserId && p.HikeRoomId == _roomId);

            bool canEditEquipment = currentUser?.EditEquipment ?? false;

            if (canEditEquipment)
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

            // Кнопка удаления (только если есть права)
            if (canEditEquipment)
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
            // Получаем данные из тега кнопки
            if (sender is Button button &&
                button.Tag is object tagObj &&
                tagObj.GetType().GetProperty("Item")?.GetValue(tagObj) is EquipmentQuantityItem item &&
                tagObj.GetType().GetProperty("ParticipantId")?.GetValue(tagObj) is int participantId)
            {
                // Получаем список снаряжения именно для этого участника
                if (_participantEquipment.TryGetValue(participantId, out var equipmentList))
                {
                    equipmentList.Remove(item);
                    UpdateEquipmentUI(participantId); // Обновляем интерфейс для этого участника
                }
            }
        }
        // Обновление статистики снаряжения
        private void UpdateEquipmentStats(int participantId, List<EquipmentQuantityItem> equipmentList)
        {
            // Вычисляем общий вес и количество предметов для данного участника
            int totalWeight = 0;
            int totalItems = 0;

            foreach (var item in equipmentList)
            {
                totalWeight += item.Equipment.Weight * item.Quantity;
                totalItems += item.Quantity;
            }

            // Ищем элементы статистики ТОЛЬКО для данного участника
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
        // Поиск стек панели для снаряжения по ID участника (ищем по тегу)
        private StackPanel FindEquipmentStackPanel(int participantId)
        {
            foreach (var item in ParticipantsItemsControl.Items)
            {
                var container = ParticipantsItemsControl.ItemContainerGenerator.ContainerFromItem(item);
                if (container == null) continue;

                // Ищем ТОЛЬКО панели с тегом = participantId
                var panels = FindAllVisualChildren<StackPanel>(container);
                foreach (var panel in panels)
                {
                    if (panel.Tag?.ToString() == participantId.ToString())
                    {
                        return panel;
                    }
                }
            }
            return null;
        }
        // Вспомогательный метод для поиска ВСЕХ дочерних элементов заданного типа
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
        // Вспомогательный метод для поиска дочернего элемента 
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

                if (NavigationService == null)
                {
                    MessageBox.Show("Ошибка навигации: NavigationService недоступен", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                NavigationService.Navigate(new CatalogEquipmentsPage(this));
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

                    // Получаем ВСЕ существующие записи снаряжения для этой комнаты из БД
                    var existingRecords = ctx.HikeRoomsEquipments
                        .Where(hre => hre.HikeRoomId == _roomId)
                        .ToList();

                    // Обрабатываем каждого участника
                    foreach (var item in ParticipantsItemsControl.Items)
                    {
                        if (item is Participant participant)
                        {
                            int participantId = participant.ParticipantId;

                            if (_participantEquipment.TryGetValue(participantId, out var equipmentList))
                            {
                                // 1. Добавляем/обновляем снаряжение из интерфейса
                                foreach (var eqItem in equipmentList)
                                {
                                    if (eqItem.Quantity <= 0)
                                        continue; // Пропускаем записи с количеством 0 или меньше

                                    // Ищем существующую запись для этого снаряжения у участника
                                    var existingRecord = existingRecords
                                        .FirstOrDefault(hre =>
                                            hre.ParticipantId == participantId &&
                                            hre.EquipmentId == eqItem.Equipment.EquipmentId);

                                    if (existingRecord != null)
                                    {
                                        // Обновляем существующую запись
                                        existingRecord.Quantity = eqItem.Quantity;
                                        updatedCount++;
                                    }
                                    else
                                    {
                                        // Создаём новую запись
                                        var newRecord = new HikeRoomsEquipment
                                        {
                                            HikeRoomId = _roomId,
                                            ParticipantId = participantId,
                                            EquipmentId = eqItem.Equipment.EquipmentId,
                                            Quantity = eqItem.Quantity
                                        };
                                        ctx.HikeRoomsEquipments.Add(newRecord);
                                        addedCount++;
                                    }
                                }

                                // 2. Удаляем записи, которых НЕТ в интерфейсе (удалённые пользователем)
                                var recordsToDelete = existingRecords
                                    .Where(hre => hre.ParticipantId == participantId &&
                                                 !equipmentList.Any(eq =>
                                                     eq.Equipment.EquipmentId == hre.EquipmentId &&
                                                     eq.Quantity > 0)) // Учитываем только записи с количеством > 0
                                    .ToList();

                                foreach (var record in recordsToDelete)
                                {
                                    ctx.HikeRoomsEquipments.Remove(record);
                                    deletedCount++;
                                }
                            }
                        }
                    }

                    ShowMessage(SaveSuccessMessage, "Изменения сохранены", true);

                    ctx.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении снаряжения:\n{ex.ToString()}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void UpdateEquipmentPermissions(bool canEditEquipment)
        {
            // Обновляем все контейнеры участников
            foreach (var item in ParticipantsItemsControl.Items)
            {
                var container = ParticipantsItemsControl.ItemContainerGenerator.ContainerFromItem(item);
                if (container == null) continue;

                // Ищем кнопку "Добавить снаряжение" по ToolTip
                var addButton = FindVisualChild<Button>(container, btn =>
                    btn.ToolTip != null && btn.ToolTip.ToString() == "Добавить снаряжение");

                if (addButton != null)
                {
                    addButton.Visibility = canEditEquipment ? Visibility.Visible : Visibility.Collapsed;
                }

                // Если нет прав, скрываем все кнопки удаления для этого участника
                if (!canEditEquipment)
                {
                    var removeButtons = FindAllVisualChildren<Button>(container)
                        .Where(btn => btn.ToolTip?.ToString() == "Удалить снаряжение");

                    foreach (var btn in removeButtons)
                    {
                        btn.Visibility = Visibility.Collapsed;
                    }

                    // Делаем поля количества только для чтения
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

        // === 3. Продуктовая раскладка ===
        private void UpdateCurrentDayTitle(int dayNumber)
        {
            var currentDay = _allDays.FirstOrDefault(d => d.DayNumber == dayNumber);
            if (currentDay != null)
            {
                CurrentDayTitle.Text = $"День {dayNumber}: {currentDay.Date:dd MMMM yyyy}";
            }
        }
        private void ShowCurrentDay(int dayNumber)
        {
            var currentDay = _allDays.FirstOrDefault(d => d.DayNumber == dayNumber);
            if (currentDay != null)
            {
                // Отображаем ТОЛЬКО один день в контенте
                DaysContentControl.ItemsSource = new List<DayViewModel> { currentDay };
                _currentDay = dayNumber;

                // Обновляем заголовок
                UpdateCurrentDayTitle(dayNumber);

                // Активируем кнопку с задержкой для гарантии рендеринга
                Dispatcher.BeginInvoke(() =>
                {
                    ActivateDayButton(dayNumber);
                    // Обновляем права для вновь созданного контейнера
                    UpdateMealPlanPermissions(_canEditMealPlan);
                }, System.Windows.Threading.DispatcherPriority.Render);
            }
        }
        private void ActivateDayButton(int dayNumber)
        {
            // Ждем пока контейнеры создадутся
            Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    for (int i = 0; i < DaysButtonsControl.Items.Count; i++)
                    {
                        var container = DaysButtonsControl.ItemContainerGenerator.ContainerFromIndex(i);
                        if (container != null)
                        {
                            var button = FindVisualChild<Button>(container);
                            if (button != null)
                            {
                                var dayVm = (DayViewModel)DaysButtonsControl.Items[i];
                                if (dayVm.DayNumber == dayNumber)
                                {
                                    Debug.WriteLine($"Найдена кнопка дня {dayNumber}, активируем");
                                    button.Background = new SolidColorBrush(Color.FromRgb(52, 152, 219));
                                    button.Foreground = Brushes.White;
                                }
                                else
                                {
                                    button.Background = Brushes.White;
                                    button.Foreground = new SolidColorBrush(Color.FromRgb(44, 62, 80));
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Ошибка в ActivateDayButton: {ex.Message}");
                }
            }), System.Windows.Threading.DispatcherPriority.Render);
        }
        private void DayButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int dayNumber)
            {
                ShowCurrentDay(dayNumber);
            }
        }
        private T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            var parentObject = VisualTreeHelper.GetParent(child);

            if (parentObject is T parent)
                return parent;

            if (parentObject == null)
                return null;

            return FindParent<T>(parentObject);
        }
        private void AddRecipeButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string mealTypeTag)
            {
                _currentMealTypeTag = mealTypeTag;
                NavigationService?.Navigate(new CatalogRecipesPage(this, _roomId));
            }
        }
        public void OnRecipeSelected(Recipe recipe, List<int> responsibleParticipantIds)
        {
            try
            {
                int currentDayToRestore = _currentDay;

                using var ctx = new TrackSyncContext();
                var room = ctx.HikeRooms.Find(_roomId);
                if (room == null)
                {
                    MessageBox.Show("Ошибка: Комната не найдена", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Парсим тег
                var parts = _currentMealTypeTag.Split('_');
                string mealType = parts[0];
                int dayNumber = int.Parse(parts[1].Replace("day", ""));

                int typeMealId = mealType switch
                {
                    "breakfast" => 1,
                    "lunch" => 2,
                    "dinner" => 3,
                    "snack" => 4,
                    _ => 1
                };

                DateOnly mealDate = room.StartDate.AddDays(dayNumber - 1);

                // Ищем существующую запись рецепта для этого приёма пищи
                var existing = ctx.HikeRoomsRecipes
                    .FirstOrDefault(hrr => hrr.HikeRoomId == _roomId &&
                                         hrr.MealDate == mealDate &&
                                         hrr.TypeMealId == typeMealId);

                if (existing != null)
                {
                    // Получаем участников этой комнаты
                    var participantsInRoom = ctx.Participants
                        .Where(p => p.HikeRoomId == _roomId)
                        .Select(p => p.ParticipantId)
                        .ToList();

                    // Удаляем только связи с участниками этой комнаты для ЭТОГО РЕЦЕПТА
                    var oldResponsibles = ctx.RecipesParticipants
                        .Where(rp => rp.RecipeId == existing.RecipeId &&
                                   participantsInRoom.Contains(rp.ParticipantId))
                        .ToList();

                    ctx.RecipesParticipants.RemoveRange(oldResponsibles);

                    // Удаляем запись о рецепте в приёме пищи
                    ctx.HikeRoomsRecipes.Remove(existing);
                    ctx.SaveChanges(); // Сохраняем удаление
                }

                // Добавляем новую запись рецепта для приёма пищи
                var newRecipeEntry = new HikeRoomsRecipe
                {
                    HikeRoomId = _roomId,
                    RecipeId = recipe.RecipeId,
                    TypeMealId = typeMealId,
                    MealDate = mealDate
                };
                ctx.HikeRoomsRecipes.Add(newRecipeEntry);
                ctx.SaveChanges(); // Сохраняем чтобы получить ID

                // Добавляем новых ответственных (только если участник в этой комнате)
                foreach (var participantId in responsibleParticipantIds)
                {
                    // Проверяем, что участник именно из этой комнаты
                    var participantInRoom = ctx.Participants
                        .Any(p => p.ParticipantId == participantId &&
                                 p.HikeRoomId == _roomId);

                    if (participantInRoom)
                    {
                        // Проверяем, не существует ли уже такая связь
                        var existingLink = ctx.RecipesParticipants
                            .FirstOrDefault(rp => rp.RecipeId == recipe.RecipeId &&
                                                 rp.ParticipantId == participantId);

                        if (existingLink == null)
                        {
                            ctx.RecipesParticipants.Add(new RecipesParticipant
                            {
                                RecipeId = recipe.RecipeId,
                                ParticipantId = participantId
                            });
                        }
                    }
                }

                ctx.SaveChanges();

                // Перезагружаем раскладку
                LoadMealPlan();
                ShowCurrentDay(currentDayToRestore);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении рецепта:\n{ex.Message}\n{ex.InnerException?.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void UpdateMealPlanPermissions(bool canEditMealPlan)
        {
            // Подписываемся на событие создания контейнеров элементов
            DaysContentControl.ItemContainerGenerator.StatusChanged += (sender, args) =>
            {
                if (DaysContentControl.ItemContainerGenerator.Status == System.Windows.Controls.Primitives.GeneratorStatus.ContainersGenerated)
                {
                    // Обновляем кнопки для всех созданных контейнеров
                    for (int i = 0; i < DaysContentControl.Items.Count; i++)
                    {
                        var container = DaysContentControl.ItemContainerGenerator.ContainerFromIndex(i);
                        if (container != null)
                        {
                            // Ищем все кнопки "Добавить рецепт"
                            var addRecipeButtons = FindAllVisualChildren<Button>(container)
                                .Where(btn =>
                                    (btn.Content is TextBlock textBlock && textBlock.Text == "Добавить рецепт") ||
                                    (btn.Content is string contentString && contentString == "Добавить рецепт") ||
                                    (btn.Tag != null && btn.Tag.ToString().Contains("recipe")))
                                .ToList();

                            foreach (var btn in addRecipeButtons)
                            {
                                btn.Visibility = canEditMealPlan ? Visibility.Visible : Visibility.Collapsed;
                            }
                        }
                    }
                }
            };

            // Также сразу проверяем, если контейнеры уже созданы
            Dispatcher.BeginInvoke(new Action(() =>
            {
                for (int i = 0; i < DaysContentControl.Items.Count; i++)
                {
                    var container = DaysContentControl.ItemContainerGenerator.ContainerFromIndex(i);
                    if (container != null)
                    {
                        // Ищем все кнопки "Добавить рецепт"
                        var addRecipeButtons = FindAllVisualChildren<Button>(container)
                            .Where(btn =>
                                (btn.Content is TextBlock textBlock && textBlock.Text == "Добавить рецепт") ||
                                (btn.Content is string contentString && contentString == "Добавить рецепт") ||
                                (btn.Tag != null && btn.Tag.ToString().Contains("recipe")))
                            .ToList();

                        foreach (var btn in addRecipeButtons)
                        {
                            btn.Visibility = canEditMealPlan ? Visibility.Visible : Visibility.Collapsed;
                        }
                    }
                }
            }), System.Windows.Threading.DispatcherPriority.Render);
        }

        // === 4. Ингердиенты ===
        private void RoomTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Проверяем, выбрана ли вкладка "Статистика ингредиентов"
            if (RoomTabControl.SelectedItem is TabItem selectedTab &&
                selectedTab.Header?.ToString() == "Статистика ингредиентов")
            {
                LoadIngredientsStatistics();
            }
        }

        // === Дополнительные кнопки ===
        private void LogoutButton_Click(object sender, EventArgs e)
        {
            FrameContext.MainWindowFrame.Navigate(new MainPage());
        }
        private void InviteUsersButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new InviteUsersPage(_roomId));
        }
        private void RoomSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new RoomSettingsPage(_roomId));
        }
        private void ShowMessage(TextBlock messageBlock, string message, bool isSuccess)
        {
            messageBlock.Visibility = Visibility.Visible;
            messageBlock.Text = message;
            messageBlock.Foreground = isSuccess ?
                (System.Windows.Media.Brushes.Green) :
                (System.Windows.Media.Brushes.Red);

            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            timer.Tick += (s, args) =>
            {
                messageBlock.Visibility = Visibility.Collapsed;
                timer.Stop();
            };
            timer.Start();
        }
        private RenderTargetBitmap GetMapScreenshot()
        {
            try
            {
                // Ждем, чтобы карта полностью отрендерилась
                MyRouteBuilder.MapControl.Dispatcher.Invoke(() => { }, DispatcherPriority.Render);

                // Используем текущий размер карты
                double actualWidth = MyRouteBuilder.MapControl.ActualWidth;
                double actualHeight = MyRouteBuilder.MapControl.ActualHeight;

                //Если размеры не определены, используем значения по умолчанию
                if (actualWidth <= 0 || actualHeight <= 0)
                {
                    actualWidth = 1123;
                    actualHeight = 793;
                }

                // Создаем скриншот текущего состояния карты
                var renderTargetBitmap = new RenderTargetBitmap(
                    (int)actualWidth,
                    (int)actualHeight,
                    96, 96, PixelFormats.Pbgra32);

                renderTargetBitmap.Render(MyRouteBuilder.MapControl);

                return renderTargetBitmap;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка создания скриншота карты: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        private void DownloadPdfButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Получаем скриншот карты
                var mapScreenshot = GetMapScreenshot();

                if (mapScreenshot == null)
                {
                    MessageBox.Show("Не удалось создать скриншот карты",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Получаем название комнаты для имени файла
                string roomTitle = RoomTitleText.Text;
                string safeFileName = RemoveInvalidFileNameChars(roomTitle);

                // Получаем текущий зум карты
                double mapZoom = MyRouteBuilder.MapControl.Zoom;

                // Создаём генератор с зумом
                var generator = new HikeRoomPdfGenerator(_roomId, mapScreenshot, mapZoom);

                // Диалог сохранения - только название похода без дат
                var saveDialog = new SaveFileDialog
                {
                    Filter = "PDF files (*.pdf)|*.pdf",
                    FileName = $"{safeFileName}.pdf", // Только название, без даты
                    DefaultExt = ".pdf"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    Mouse.OverrideCursor = Cursors.Wait;
                    try
                    {
                        generator.GeneratePdf(saveDialog.FileName);
                        MessageBox.Show($"PDF успешно создан!\nФайл сохранен: {saveDialog.FileName}",
                            "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка генерации PDF:\n{ex.Message}", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    finally
                    {
                        Mouse.OverrideCursor = null;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка подготовки PDF:\n{ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DownloadGpxButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Получаем все треки комнаты
                var allTracks = TrackSyncManagerList.GetAllTracksForRoom(_roomId);

                if (allTracks == null || !allTracks.Any())
                {
                    MessageBox.Show("В комнате нет сохраненных маршрутов для экспорта",
                        "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Создаем контекстное меню для выбора
                var contextMenu = new ContextMenu();

                var allTracksItem = new MenuItem
                {
                    Header = "Все маршруты комнаты",
                    Tag = "all"
                };
                allTracksItem.Click += (s, args) => ExportGpx(false);

                var currentTrackItem = new MenuItem
                {
                    Header = "Только мой маршрут",
                    Tag = "current"
                };
                currentTrackItem.Click += (s, args) => ExportGpx(true);

                contextMenu.Items.Add(allTracksItem);
                contextMenu.Items.Add(currentTrackItem);

                contextMenu.PlacementTarget = sender as Button;
                contextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
                contextMenu.IsOpen = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при экспорте GPX:\n{ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportGpx(bool onlyCurrentUser)
        {
            try
            {
                var allTracks = TrackSyncManagerList.GetAllTracksForRoom(_roomId);

                if (allTracks == null || !allTracks.Any())
                {
                    MessageBox.Show("Нет маршрутов для экспорта",
                        "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                if (onlyCurrentUser)
                {
                    int currentParticipantId = TrackSyncManagerList.GetCurrentParticipantId(
                        SessionManager.CurrentUserId, _roomId);
                    allTracks = allTracks.Where(t => t.ParticipantId == currentParticipantId).ToList();
                }

                if (!allTracks.Any())
                {
                    MessageBox.Show("Нет маршрутов для экспорта",
                        "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Диалог сохранения
                var saveDialog = new SaveFileDialog
                {
                    Filter = "GPX files (*.gpx)|*.gpx",
                    FileName = onlyCurrentUser ?
                        $"Мой_маршрут_{DateTime.Now:yyyy-MM-dd}.gpx" :
                        $"Маршруты_{RemoveInvalidFileNameChars(RoomTitleText.Text)}_{DateTime.Now:yyyy-MM-dd}.gpx",
                    DefaultExt = ".gpx"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    Mouse.OverrideCursor = Cursors.Wait;
                    try
                    {
                        string gpxContent = GenerateGpxContent(allTracks);

                        if (gpxContent == null)
                        {
                            MessageBox.Show("Не удалось сгенерировать GPX файл",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }

                        File.WriteAllText(saveDialog.FileName, gpxContent, Encoding.UTF8);

                        MessageBox.Show($"GPX файл успешно сохранен!\n{saveDialog.FileName}",
                            "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка создания GPX файла:\n{ex.Message}",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    finally
                    {
                        Mouse.OverrideCursor = null;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при экспорте GPX:\n{ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GenerateGpxContent(List<Track> tracks)
        {
            try
            {
                var gpxNs = XNamespace.Get("http://www.topografix.com/GPX/1/1");

                var doc = new XDocument(
                    new XDeclaration("1.0", "UTF-8", null),
                    new XElement(gpxNs + "gpx",
                        new XAttribute("version", "1.1"),
                        new XAttribute("creator", "TrackSync"),
                        new XAttribute(XNamespace.Xmlns + "xsi", "http://www.w3.org/2001/XMLSchema-instance"),
                        new XElement(gpxNs + "metadata",
                            new XElement(gpxNs + "name", EscapeXml($"Маршруты комнаты: {RoomTitleText.Text}")),
                            new XElement(gpxNs + "desc", EscapeXml($"Экспортировано из TrackSync {DateTime.Now:yyyy-MM-dd HH:mm}")),
                            new XElement(gpxNs + "time", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"))
                        ),
                        // Добавляем все треки как отдельные элементы
                        tracks.Select(track => CreateTrackElement(track, gpxNs)).Where(x => x != null)
                    )
                );

                return doc.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка генерации GPX: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        private XElement CreateTrackElement(Track track, XNamespace gpxNs)
        {
            var waypoints = ExtractWaypoints(track);
            if (waypoints.Count < 2) return null;

            // Создаем уникальное имя для каждого трека
            string creatorName = track.Participant?.User?.Name ?? "Неизвестно";
            string trackName = string.IsNullOrWhiteSpace(track.Title)
                ? $"Маршрут от {creatorName}"
                : track.Title;

            // Уникальное описание с информацией о создателе
            string trackDesc = $"Создатель: {creatorName}, Цвет: {track.Color}, Точки: {waypoints.Count}";

            return new XElement(gpxNs + "trk",
                new XElement(gpxNs + "name", EscapeXml(trackName)),
                new XElement(gpxNs + "desc", EscapeXml(trackDesc)),
                new XElement(gpxNs + "type", "Hiking"),
                // Добавляем расширения для цвета (нестандартное поле, но многие программы его поддерживают)
                new XElement(gpxNs + "extensions",
                    new XElement("color", track.Color ?? "#000000"),
                    new XElement("creator", EscapeXml(creatorName))
                ),
                new XElement(gpxNs + "trkseg",
                    waypoints.Select((wp, index) => CreateTrackPointElement(wp, index, gpxNs))
                )
            );
        }

        private XElement CreateTrackPointElement(Waypoint waypoint, int index, XNamespace gpxNs)
        {
            return new XElement(gpxNs + "trkpt",
                new XAttribute("lat", waypoint.Latitude.ToString(CultureInfo.InvariantCulture)),
                new XAttribute("lon", waypoint.Longitude.ToString(CultureInfo.InvariantCulture)),
                new XElement(gpxNs + "ele", waypoint.Height),
                new XElement(gpxNs + "time", DateTime.UtcNow.AddMinutes(index).ToString("yyyy-MM-ddTHH:mm:ssZ")),
                new XElement(gpxNs + "name", $"Точка {index + 1}"),
                new XElement(gpxNs + "desc", $"Высота: {waypoint.Height} м")
            );
        }

        private string EscapeXml(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;

            return s
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&apos;");
        }

        private List<Waypoint> ExtractWaypoints(Track track)
        {
            var waypoints = new List<Waypoint>();

            // Проверяем, что сегменты вообще есть
            if (track?.Segments == null || !track.Segments.Any()) return waypoints;

            // Сортируем строго по ID, чтобы не было "паутины"
            var sortedSegments = track.Segments.OrderBy(s => s.SegmentId).ToList();

            // Берем начальную точку первого сегмента
            var first = sortedSegments.First();
            if (first.StartWaypoint != null)
            {
                waypoints.Add(first.StartWaypoint);
            }

            // Добавляем конечные точки всех сегментов по порядку
            foreach (var s in sortedSegments)
            {
                if (s.EndWaypoint != null)
                {
                    waypoints.Add(s.EndWaypoint);
                }
            }

            return waypoints;
        }

        // Обновляем существующий метод ExtractPoints, чтобы он использовал ExtractWaypoints
        private List<GMap.NET.PointLatLng> ExtractPoints(Track track)
        {
            var waypoints = ExtractWaypoints(track);
            return waypoints.Select(wp => new GMap.NET.PointLatLng((double)wp.Latitude, (double)wp.Longitude)).ToList();
        }

        // Вспомогательный метод для удаления недопустимых символов из имени файла
        private string RemoveInvalidFileNameChars(string fileName)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            return new string(fileName.Where(ch => !invalidChars.Contains(ch)).ToArray());
        }

        private void ToggleDescriptionButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is MealViewModel meal)
            {
                // Проверяем, есть ли рецепт и описание
                if (meal.Recipe == null || string.IsNullOrWhiteSpace(meal.RecipeDescription))
                {
                    return; // Ничего не делаем, если нет рецепта или описания
                }

                // Найти DescriptionText в визуальном дереве
                var container = button.Parent as StackPanel;
                if (container != null)
                {
                    var descriptionText = container.Children
                        .OfType<TextBlock>()
                        .FirstOrDefault(tb => tb.Name == "DescriptionText");

                    if (descriptionText != null)
                    {
                        descriptionText.Visibility = descriptionText.Visibility == Visibility.Visible ?
                            Visibility.Collapsed :
                            Visibility.Visible;

                        // Изменить текст кнопки
                        var arrowText = (button.Content as StackPanel)?.Children
                            .OfType<TextBlock>()
                            .LastOrDefault();

                        if (arrowText != null)
                        {
                            arrowText.Text = descriptionText.Visibility == Visibility.Visible ?
                                "Закрыть описание рецепта" :
                                "Описание рецепта";
                        }
                    }
                }
            }
        }
    }
}