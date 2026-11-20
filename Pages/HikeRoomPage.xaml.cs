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
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml.Linq;
using TrackSync.DataBase;
using TrackSync.DataBase.Model;
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

        const double EquatorMetersPerPixel = 156543.03392;

        private bool _canEditMealPlan = false;
        private bool _canEditEquipment = false;
        private bool _canEditTrack = false;

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

            public int TotalCalories => Recipe != null ? CalculateTotalNutrients().Calories : 0;
            public int TotalProteins => Recipe != null ? CalculateTotalNutrients().Proteins : 0;
            public int TotalFats => Recipe != null ? CalculateTotalNutrients().Fats : 0;
            public int TotalCarbs => Recipe != null ? CalculateTotalNutrients().Carbs : 0;
            public int TotalWeight => Recipe != null ? CalculateTotalWeight() : 0;
            public int WeightPerPerson => Recipe != null ? CalculateWeightPerPerson() : 0;

            public int CaloriesPerPerson => Recipe != null ? CalculatePerPersonNutrients().Calories : 0;
            public int ProteinsPerPerson => Recipe != null ? CalculatePerPersonNutrients().Proteins : 0;
            public int FatsPerPerson => Recipe != null ? CalculatePerPersonNutrients().Fats : 0;
            public int CarbsPerPerson => Recipe != null ? CalculatePerPersonNutrients().Carbs : 0;

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
                        calories += (ir.Ingredient.Calories * ir.Weight) / 100;
                        proteins += (ir.Ingredient.Proteins * ir.Weight) / 100;
                        fats += (ir.Ingredient.Fats * ir.Weight) / 100;
                        carbs += (ir.Ingredient.Carbs * ir.Weight) / 100;
                    }
                }

                return (calories, proteins, fats, carbs);
            }

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

            _canEditMealPlan = canEditMealPlan;
            _canEditEquipment = canEditEquipment;
            _canEditTrack = canEditTrack;

            SaveRouteButton.Visibility = canEditTrack ? Visibility.Visible : Visibility.Collapsed;
            UndoRouteButton.Visibility = canEditTrack ? Visibility.Visible : Visibility.Collapsed;
            ClearRouteButton.Visibility = canEditTrack ? Visibility.Visible : Visibility.Collapsed;
            MyRouteBuilder.SetPermissions(canEditTrack);

            SaveChangesButton.Visibility = canEditEquipment ? Visibility.Visible : Visibility.Collapsed;

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

            UpdateEquipmentPermissions(canEditEquipment);

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

            int totalDays = (room.EndDate.ToDateTime(TimeOnly.MinValue) -
                           room.StartDate.ToDateTime(TimeOnly.MinValue)).Days + 1;

            var mealPlanData = TrackSyncManagerList.GetMealPlanForRoom(_roomId);

            if (mealPlanData == null || !mealPlanData.Any())
            {
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

                if (_allDays.Count < totalDays)
                {
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

                    _allDays = _allDays.OrderBy(d => d.DayNumber).ToList();
                }

                Dispatcher.BeginInvoke(() =>
                {
                    ApplyPermissions();
                }, System.Windows.Threading.DispatcherPriority.Render);

            }

            DaysButtonsControl.ItemsSource = null;
            DaysButtonsControl.ItemsSource = _allDays;

            Dispatcher.BeginInvoke(() =>
            {
                ApplyPermissions();
            }, System.Windows.Threading.DispatcherPriority.Render);

            ShowCurrentDay(1);

            Dispatcher.BeginInvoke(() =>
            {
                ActivateDayButton(1);
            }, System.Windows.Threading.DispatcherPriority.Render);
        }
        private void LoadParticipants()
        {
            var participants = TrackSyncManagerList.GetParticipantsWithEquipment(_roomId);

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
                        int attempts = 0;
                        var timer = new System.Windows.Threading.DispatcherTimer
                        {
                            Interval = TimeSpan.FromMilliseconds(50)
                        };

                        timer.Tick += (sender, args) =>
                        {
                            attempts++;

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

                            if (allContainersReady || attempts > 20)
                            {
                                timer.Stop();

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

        private void LoadIngredientsStatistics()
        {
            try
            {
                using var ctx = new TrackSyncContext();

                int participantsCount = ctx.Participants
                    .Count(p => p.HikeRoomId == _roomId);

                if (ParticipantsCountRun != null)
                    ParticipantsCountRun.Text = participantsCount.ToString();

                var roomRecipes = ctx.HikeRoomsRecipes
                    .Include(hrr => hrr.Recipe)
                        .ThenInclude(r => r.IngredientsRecipes)
                            .ThenInclude(ir => ir.Ingredient)
                    .Where(hrr => hrr.HikeRoomId == _roomId)
                    .ToList();

                if (!roomRecipes.Any())
                {
                    NoIngredientsText.Visibility = Visibility.Visible;
                    IngredientsStatisticsList.ItemsSource = null;
                    TotalCostText.Text = "0 руб.";
                    CostPerPersonText.Text = "0 руб.";
                    return;
                }

                NoIngredientsText.Visibility = Visibility.Collapsed;

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

                        int totalWeightForAllParticipants = ingredientRecipe.Weight * participantsCount;

                        decimal costPer100g = ingredient.Price;
                        decimal costForIngredient = (costPer100g * totalWeightForAllParticipants) / 100;

                        if (ingredientStats.ContainsKey(ingredient.IngredientId))
                        {
                            var existing = ingredientStats[ingredient.IngredientId];
                            existing.TotalWeight += totalWeightForAllParticipants;
                            existing.TotalCost += costForIngredient;
                            recipeUsageCount[ingredient.IngredientId]++;
                        }
                        else
                        {
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

                foreach (var stat in ingredientStats.Values)
                {
                    stat.UsedInRecipesCount = recipeUsageCount[stat.IngredientId];

                    stat.CostPerPerson = participantsCount > 0 ? stat.TotalCost / participantsCount : 0;
                }

                var sortedStats = ingredientStats.Values
                    .OrderByDescending(i => i.TotalCost)
                    .ToList();

                IngredientsStatisticsList.ItemsSource = sortedStats;

                decimal totalCost = sortedStats.Sum(i => i.TotalCost);
                decimal costPerPerson = participantsCount > 0 ? totalCost / participantsCount : 0;

                TotalCostText.Text = $"{totalCost:F2} руб.";
                CostPerPersonText.Text = $"{costPerPerson:F2} руб.";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке статистики ингредиентов:\n{ex.Message}",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //  1. Маршруты
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

                        MyRouteBuilder.LoadRoute(points, MyRouteBuilder.SelectedColor, centerMap: false);
                        ShowMessage(MapMessage, "Маршрут сохранён", true);

                        _isEditingRoute = false;
                        _editingRouteStat = null;
                        _currentEditingSessionId = Guid.NewGuid();

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
                            string trackColor = !string.IsNullOrEmpty(track.Color) ? track.Color : "#FF0000";
                            MyRouteBuilder.LoadRoute(points, trackColor, centerMap: true);

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

                MyRouteBuilder.DisableUsedColors(usedColors);

                if (allPoints.Count > 0)
                {
                    MyRouteBuilder.CenterMapOnRoutes(allPoints);
                }
                else
                {
                    MyRouteBuilder.CenterMapOnPoint(new GMap.NET.PointLatLng(55.7558, 37.6173), 10);
                }

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

        private void UpdateRoutesStatistics()
        {
            try
            {
                var stats = new List<RouteStatItem>();
                int myPartId = TrackSyncManagerList.GetCurrentParticipantId(SessionManager.CurrentUserId, _roomId);

                if (_isEditingRoute && _editingRouteStat != null && _editingRouteStat.PointsCount > 0)
                {
                    stats.Add(_editingRouteStat);
                }

                var allTracks = TrackSyncManagerList.GetAllTracksForRoom(_roomId);

                foreach (var track in allTracks)
                {
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

                stats = stats.OrderBy(s => s.CreatorName).ToList();

                RoutesStatisticsList.ItemsSource = null;
                RoutesStatisticsList.ItemsSource = stats;
            }
            catch (Exception ex)
            {
                RoutesStatisticsList.ItemsSource = null;
            }
        }

        private void UndoRouteButton_Click(object sender, RoutedEventArgs e)
        {
            int pointsBefore = MyRouteBuilder.RoutePoints.Count;

            if (pointsBefore == 0)
            {
                return;
            }

            MyRouteBuilder.UndoLastPoint();

            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (MyRouteBuilder.RoutePoints.Count == 0 && _isEditingRoute)
                {
                    // Получаем ID текущего участника
                    int myPartId = TrackSyncManagerList.GetCurrentParticipantId(SessionManager.CurrentUserId, _roomId);

                    _isEditingRoute = false;
                    _editingRouteStat = null;
                    _currentEditingSessionId = Guid.NewGuid();

                    var allTracks = TrackSyncManagerList.GetAllTracksForRoom(_roomId);
                    var stats = new List<RouteStatItem>();

                    foreach (var track in allTracks)
                    {
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
                int myPartId = TrackSyncManagerList.GetCurrentParticipantId(SessionManager.CurrentUserId, _roomId);

                MyRouteBuilder.Clear();

                _isEditingRoute = false;
                _editingRouteStat = null;

                _currentEditingSessionId = Guid.NewGuid();

                var allTracks = TrackSyncManagerList.GetAllTracksForRoom(_roomId);
                var stats = new List<RouteStatItem>();

                foreach (var track in allTracks)
                {
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

                stats = stats.OrderBy(s => s.CreatorName).ToList();
                RoutesStatisticsList.ItemsSource = null;
                RoutesStatisticsList.ItemsSource = stats;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при очистке маршрута: {ex.Message}");
            }
        }

        // 2. Снаряжение
        public void OnEquipmentSelected(List<Equipment> equipment)
        {
            if (equipment == null || equipment.Count == 0)
                return;

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

            UpdateEquipmentUI(_currentParticipantId);
        }

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
                    if (panel.Tag?.ToString() == participantId.ToString())
                    {
                        return panel;
                    }
                }
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

                    var existingRecords = ctx.HikeRoomsEquipments
                        .Where(hre => hre.HikeRoomId == _roomId)
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
                                            HikeRoomId = _roomId,
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
            foreach (var item in ParticipantsItemsControl.Items)
            {
                var container = ParticipantsItemsControl.ItemContainerGenerator.ContainerFromItem(item);
                if (container == null) continue;

                var addButton = FindVisualChild<Button>(container, btn =>
                    btn.ToolTip != null && btn.ToolTip.ToString() == "Добавить снаряжение");

                if (addButton != null)
                {
                    addButton.Visibility = canEditEquipment ? Visibility.Visible : Visibility.Collapsed;
                }

                if (!canEditEquipment)
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

        //  3. Продуктовая раскладка 
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
                DaysContentControl.ItemsSource = new List<DayViewModel> { currentDay };
                _currentDay = dayNumber;

                UpdateCurrentDayTitle(dayNumber);

                Dispatcher.BeginInvoke(() =>
                {
                    ActivateDayButton(dayNumber);
                    UpdateMealPlanPermissions(_canEditMealPlan);
                }, System.Windows.Threading.DispatcherPriority.Render);
            }
        }
        private void ActivateDayButton(int dayNumber)
        {
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

                var existing = ctx.HikeRoomsRecipes
                    .FirstOrDefault(hrr => hrr.HikeRoomId == _roomId &&
                                         hrr.MealDate == mealDate &&
                                         hrr.TypeMealId == typeMealId);

                if (existing != null)
                {
                    var participantsInRoom = ctx.Participants
                        .Where(p => p.HikeRoomId == _roomId)
                        .Select(p => p.ParticipantId)
                        .ToList();

                    var oldResponsibles = ctx.RecipesParticipants
                        .Where(rp => rp.RecipeId == existing.RecipeId &&
                                   participantsInRoom.Contains(rp.ParticipantId))
                        .ToList();

                    ctx.RecipesParticipants.RemoveRange(oldResponsibles);

                    ctx.HikeRoomsRecipes.Remove(existing);
                    ctx.SaveChanges(); 
                }

                var newRecipeEntry = new HikeRoomsRecipe
                {
                    HikeRoomId = _roomId,
                    RecipeId = recipe.RecipeId,
                    TypeMealId = typeMealId,
                    MealDate = mealDate
                };
                ctx.HikeRoomsRecipes.Add(newRecipeEntry);
                ctx.SaveChanges(); 

                foreach (var participantId in responsibleParticipantIds)
                {
                    var participantInRoom = ctx.Participants
                        .Any(p => p.ParticipantId == participantId &&
                                 p.HikeRoomId == _roomId);

                    if (participantInRoom)
                    {
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
            DaysContentControl.ItemContainerGenerator.StatusChanged += (sender, args) =>
            {
                if (DaysContentControl.ItemContainerGenerator.Status == System.Windows.Controls.Primitives.GeneratorStatus.ContainersGenerated)
                {
                    for (int i = 0; i < DaysContentControl.Items.Count; i++)
                    {
                        var container = DaysContentControl.ItemContainerGenerator.ContainerFromIndex(i);
                        if (container != null)
                        {
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

            Dispatcher.BeginInvoke(new Action(() =>
            {
                for (int i = 0; i < DaysContentControl.Items.Count; i++)
                {
                    var container = DaysContentControl.ItemContainerGenerator.ContainerFromIndex(i);
                    if (container != null)
                    {
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

        //  4. Ингердиенты
        private void RoomTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            if (RoomTabControl.SelectedItem is TabItem selectedTab &&
                selectedTab.Header?.ToString() == "Статистика ингредиентов")
            {
                LoadIngredientsStatistics();
            }
        }

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
            RenderTargetBitmap result = null;

            try
            {
                MyRouteBuilder.MapControl.Dispatcher.Invoke(() =>
                {
                    try
                    {
                        MyRouteBuilder.HideAllMarkers();

                        MyRouteBuilder.MapControl.Dispatcher.Invoke(() => { }, DispatcherPriority.Render);

                        double actualWidth = MyRouteBuilder.MapControl.ActualWidth;
                        double actualHeight = MyRouteBuilder.MapControl.ActualHeight;

                        if (actualWidth <= 0 || actualHeight <= 0)
                        {
                            actualWidth = 1123;
                            actualHeight = 793;
                        }

                        result = new RenderTargetBitmap(
                            (int)actualWidth,
                            (int)actualHeight,
                            96, 96, PixelFormats.Pbgra32);

                        result.Render(MyRouteBuilder.MapControl);

                        MyRouteBuilder.ShowAllMarkers();
                    }
                    catch (Exception ex)
                    {
                        MyRouteBuilder.ShowAllMarkers();
                        throw;
                    }
                }, DispatcherPriority.Render);

                return result;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка создания скриншота карты: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }
        private void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var contextMenu = new ContextMenu();

                // Опция 1: Скачать в PDF
                var pdfItem = new MenuItem
                {
                    Header = "Скачать в PDF",
                    Tag = "pdf"
                };
                pdfItem.Click += (s, args) => ExportToPdf();
                contextMenu.Items.Add(pdfItem);

                // Опция 2: Скачать в GPX свой маршрут
                var gpxMyItem = new MenuItem
                {
                    Header = "Скачать в GPX свой маршрут",
                    Tag = "gpx_my"
                };
                gpxMyItem.Click += (s, args) => ExportCurrentUserTrack();
                contextMenu.Items.Add(gpxMyItem);

                // Опция 3: Скачать в GPX все маршруты (отдельные файлы)
                var gpxAllItem = new MenuItem
                {
                    Header = "Скачать в GPX все маршруты (отдельные файлы)",
                    Tag = "gpx_all"
                };
                gpxAllItem.Click += (s, args) => ExportAllTracksAsSeparateFiles();
                contextMenu.Items.Add(gpxAllItem);

                contextMenu.PlacementTarget = sender as Button;
                contextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
                contextMenu.IsOpen = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при отображении меню скачивания:\n{ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void ExportToPdf()
        {
            try
            {
                var mapScreenshot = GetMapScreenshot();

                if (mapScreenshot == null)
                {
                    MessageBox.Show("Не удалось создать скриншот карты",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                string roomTitle = RoomTitleText.Text;
                string safeFileName = RemoveInvalidFileNameChars(roomTitle);

                double zoom = MyRouteBuilder.MapControl.Zoom;
                double lat = MyRouteBuilder.MapControl.Position.Lat;
                double metersPerPixel = EquatorMetersPerPixel * Math.Cos(lat * Math.PI / 180) / Math.Pow(2, zoom);
                double screenDpi = 72;
                double realScale = metersPerPixel * (screenDpi / 0.0254);
                double roundedScale = Math.Round(realScale / 10000) * 10000;

                var generator = new HikeRoomPdfGenerator(_roomId, mapScreenshot, roundedScale);

                var saveDialog = new SaveFileDialog
                {
                    Filter = "PDF files (*.pdf)|*.pdf",
                    FileName = $"{safeFileName}.pdf",
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
        private string GenerateSingleTrackGpx(Track track, List<Waypoint> waypoints)
        {
            try
            {
                var gpxNs = XNamespace.Get("http://www.topografix.com/GPX/1/1");

                string creatorName = track.Participant?.User?.Name ?? "Неизвестно";
                string trackName = string.IsNullOrWhiteSpace(track.Title)
                    ? $"Маршрут от {creatorName}"
                    : track.Title;

                var doc = new XDocument(
                    new XDeclaration("1.0", "UTF-8", null),
                    new XElement(gpxNs + "gpx",
                        new XAttribute("version", "1.1"),
                        new XAttribute("creator", "TrackSync"),
                        new XAttribute(XNamespace.Xmlns + "xsi", "http://www.w3.org/2001/XMLSchema-instance"),
                        new XElement(gpxNs + "metadata",
                            new XElement(gpxNs + "name", EscapeXml(trackName)),
                            new XElement(gpxNs + "desc", EscapeXml($"Экспортировано из TrackSync {DateTime.Now:yyyy-MM-dd HH:mm}")),
                            new XElement(gpxNs + "time", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")),
                            new XElement(gpxNs + "keywords", $"маршрут, поход, {creatorName}"),
                            new XElement(gpxNs + "author",
                                new XElement(gpxNs + "name", EscapeXml(creatorName))
                            )
                        ),
                        new XElement(gpxNs + "trk",
                            new XElement(gpxNs + "name", EscapeXml(trackName)),
                            new XElement(gpxNs + "desc", EscapeXml($"Создатель: {creatorName}, Точки: {waypoints.Count}, Цвет: {track.Color}")),
                            new XElement(gpxNs + "type", "Hiking"),
                            new XElement(gpxNs + "extensions",
                                new XElement("color", track.Color ?? "#000000"),
                                new XElement("creator_id", track.ParticipantId),
                                new XElement("room_id", _roomId)
                            ),
                            new XElement(gpxNs + "trkseg",
                                waypoints.Select((wp, index) => CreateTrackPointElement(wp, index, gpxNs))
                            )
                        )
                    )
                );

                return doc.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка генерации GPX для трека {track.TrackId}: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        private XElement CreateTrackPointElement(Waypoint waypoint, int index, XNamespace gpxNs)
        {
            DateTime pointTime = DateTime.UtcNow.AddMinutes(index);

            return new XElement(gpxNs + "trkpt",
                new XAttribute("lat", waypoint.Latitude.ToString(CultureInfo.InvariantCulture)),
                new XAttribute("lon", waypoint.Longitude.ToString(CultureInfo.InvariantCulture)),
                new XElement(gpxNs + "ele", waypoint.Height),
                new XElement(gpxNs + "time", pointTime.ToString("yyyy-MM-ddTHH:mm:ssZ")),
                new XElement(gpxNs + "name", $"Точка {index + 1}"),
                new XElement(gpxNs + "desc", $"Высота: {waypoint.Height} м")
            );
        }

        private void ExportAllTracksAsSeparateFiles()
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

                var saveDialog = new SaveFileDialog
                {
                    Filter = "GPX files (*.gpx)|*.gpx",
                    FileName = $"Маршруты_{RemoveInvalidFileNameChars(RoomTitleText.Text)}",
                    DefaultExt = ".gpx",
                    Title = "Выберите папку и базовое имя для маршрутов"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    Mouse.OverrideCursor = Cursors.Wait;
                    try
                    {
                        string selectedPath = System.IO.Path.GetDirectoryName(saveDialog.FileName);
                        string baseFileName = System.IO.Path.GetFileNameWithoutExtension(saveDialog.FileName);

                        if (string.IsNullOrEmpty(selectedPath) || string.IsNullOrEmpty(baseFileName))
                        {
                            MessageBox.Show("Неверно выбран путь для сохранения",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }

                        int savedCount = 0;
                        int skippedCount = 0;
                        List<string> savedFiles = new List<string>();

                        foreach (var track in allTracks)
                        {
                            var waypoints = ExtractWaypoints(track);
                            if (waypoints.Count < 2)
                            {
                                skippedCount++;
                                continue;
                            }

                            string creatorName = track.Participant?.User?.Name ?? "Неизвестно";
                            string safeCreatorName = RemoveInvalidFileNameChars(creatorName);
                            string safeTrackName = RemoveInvalidFileNameChars(track.Title ?? $"Маршрут_{safeCreatorName}");

                            string fileName = $"{baseFileName}_{safeTrackName}_{track.TrackId}.gpx";
                            string filePath = System.IO.Path.Combine(selectedPath, fileName);

                            int counter = 1;
                            while (File.Exists(filePath))
                            {
                                fileName = $"{baseFileName}_{safeTrackName}_{track.TrackId}_{counter}.gpx";
                                filePath = System.IO.Path.Combine(selectedPath, fileName);
                                counter++;
                            }

                            string gpxContent = GenerateSingleTrackGpx(track, waypoints);
                            File.WriteAllText(filePath, gpxContent, Encoding.UTF8);
                            savedCount++;
                            savedFiles.Add(filePath);
                        }

                        string message;
                        if (savedCount > 0)
                        {
                            message = $"Сохранено {savedCount} маршрутов в папку:\n{selectedPath}";
                            if (skippedCount > 0)
                            {
                                message += $"\nПропущено {skippedCount} маршрутов (слишком мало точек)";
                            }

                            try
                            {
                                Process.Start("explorer.exe", selectedPath);
                            }
                            catch
                            {

                            }
                        }
                        else
                        {
                            message = "Не удалось сохранить ни одного маршрута.";
                        }

                        MessageBox.Show(message,
                            savedCount > 0 ? "Успех" : "Информация",
                            MessageBoxButton.OK,
                            savedCount > 0 ? MessageBoxImage.Information : MessageBoxImage.Warning);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка создания файлов:\n{ex.Message}",
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

        private void ExportCurrentUserTrack()
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

                int currentParticipantId = TrackSyncManagerList.GetCurrentParticipantId(
                    SessionManager.CurrentUserId, _roomId);

                var currentUserTrack = allTracks.FirstOrDefault(t => t.ParticipantId == currentParticipantId);

                if (currentUserTrack == null)
                {
                    MessageBox.Show("У вас нет сохраненного маршрута в этой комнате",
                        "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var waypoints = ExtractWaypoints(currentUserTrack);
                if (waypoints.Count < 2)
                {
                    MessageBox.Show("Ваш маршрут содержит слишком мало точек для экспорта",
                        "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var saveDialog = new SaveFileDialog
                {
                    Filter = "GPX files (*.gpx)|*.gpx",
                    FileName = $"{RemoveInvalidFileNameChars(currentUserTrack.Title ?? "Мой_маршрут")}_{DateTime.Now:yyyyMMdd}.gpx",
                    DefaultExt = ".gpx"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    Mouse.OverrideCursor = Cursors.Wait;
                    try
                    {
                        string gpxContent = GenerateSingleTrackGpx(currentUserTrack, waypoints);
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

            if (track?.Segments == null || !track.Segments.Any()) return waypoints;

            var sortedSegments = track.Segments.OrderBy(s => s.SegmentId).ToList();

            var first = sortedSegments.First();
            if (first.StartWaypoint != null)
            {
                waypoints.Add(first.StartWaypoint);
            }

            foreach (var s in sortedSegments)
            {
                if (s.EndWaypoint != null)
                {
                    waypoints.Add(s.EndWaypoint);
                }
            }

            return waypoints;
        }

        private List<GMap.NET.PointLatLng> ExtractPoints(Track track)
        {
            var waypoints = ExtractWaypoints(track);
            return waypoints.Select(wp => new GMap.NET.PointLatLng((double)wp.Latitude, (double)wp.Longitude)).ToList();
        }

        private string RemoveInvalidFileNameChars(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return fileName;

            var invalidChars = System.IO.Path.GetInvalidFileNameChars();
            var result = new string(fileName.Where(ch => !invalidChars.Contains(ch)).ToArray());

            result = result.Trim();

            if (string.IsNullOrEmpty(result))
            {
                return "Маршрут";
            }

            if (result.Length > 100)
            {
                result = result.Substring(0, 100);
            }

            return result;
        }

        private void ToggleDescriptionButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is MealViewModel meal)
            {
                if (meal.Recipe == null || string.IsNullOrWhiteSpace(meal.RecipeDescription))
                {
                    return; 
                }

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