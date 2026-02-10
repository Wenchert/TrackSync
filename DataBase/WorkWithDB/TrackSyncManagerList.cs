using Microsoft.EntityFrameworkCore;
using System.Windows;
using TrackSync.Pages;


namespace TrackSync.DataBase.WorkWithDB
{
    public static class TrackSyncManagerList
    {
        /// <summary>
        /// Получить все снаряжение из базы данных
        /// </summary>
        public static List<Equipment> GetAllEquipmentForUser(int userId)
        {
            try
            {
                using (var ctx = new TrackSyncContext())
                {
                    if (!ctx.Database.CanConnect())
                    {
                        MessageBox.Show("Ошибка: Не удалось подключиться к базе данных! Проверьте настройки подключения.", "Ошибка");
                        return null;
                    }

                    return ctx.Equipments
                        .Where(e => e.CreatorUserId == null || e.CreatorUserId == userId)
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при подключении к базе данных:\n{ex.Message}", "Ошибка");
                return null;
            }
        }

        /// <summary>
        /// Получить только снаряжение, созданное пользователем
        /// </summary>
        public static List<Equipment> GetUserEquipment(int userId)
        {
            try
            {
                using (var ctx = new TrackSyncContext())
                {
                    if (!ctx.Database.CanConnect())
                    {
                        MessageBox.Show("Ошибка: Не удалось подключиться к базе данных! Проверьте настройки подключения.", "Ошибка");
                        return null;
                    }

                    return ctx.Equipments
                        .Where(e => e.CreatorUserId == userId)
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при подключении к базе данных:\n{ex.Message}", "Ошибка");
                return null;
            }
        }

        /// <summary>
        /// Поиск снаряжения по названию
        /// </summary>
        public static List<Equipment> SearchEquipment(string searchText, int userId)
        {
            try
            {
                using (var ctx = new TrackSyncContext())
                {
                    if (!ctx.Database.CanConnect())
                    {
                        MessageBox.Show("Ошибка: Не удалось подключиться к базе данных!", "Ошибка");
                        return null;
                    }

                    if (string.IsNullOrWhiteSpace(searchText))
                    {
                        return ctx.Equipments.ToList();
                    }

                    var searchLower = searchText.ToLower();
                    return ctx.Equipments
                        .Where(e => e.Title.ToLower().Contains(searchLower) && (e.CreatorUserId == null || e.CreatorUserId == userId))
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при поиске снаряжения:\n{ex.Message}", "Ошибка");
                return null;
            }
        }

        /// <summary>
        /// Получить снаряжение по ID
        /// </summary>
        public static Equipment GetEquipmentById(int id)
        {
            try
            {
                using (var ctx = new TrackSyncContext())
                {
                    if (!ctx.Database.CanConnect())
                    {
                        MessageBox.Show("Ошибка: Не удалось подключиться к базе данных!", "Ошибка");
                        return null;
                    }

                    var equipment = ctx.Equipments.FirstOrDefault(e => e.EquipmentId == id);

                    if (equipment == null)
                    {
                        MessageBox.Show($"Снаряжение с ID {id} не найдено", "Ошибка");
                        return null;
                    }

                    return equipment;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при получении снаряжения:\n{ex.Message}", "Ошибка");
                return null;
            }
        }

        /// <summary>
        /// Получить все ингредиенты из базы данных
        /// </summary>
        public static List<Ingredient> GetAllIngredients()
        {
            try
            {
                using (var ctx = new TrackSyncContext())
                {
                    if (!ctx.Database.CanConnect())
                    {
                        MessageBox.Show("Ошибка: Не удалось подключиться к базе данных! Проверьте настройки подключения.", "Ошибка");
                        return null;
                    }

                    return ctx.Ingredients.ToList();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при подключении к базе данных:\n{ex.Message}", "Ошибка");
                return null;
            }
        }

        /// <summary>
        /// Поиск ингредиентов по названию
        /// </summary>
        public static List<Ingredient> SearchIngredients(string searchText)
        {
            try
            {
                using (var ctx = new TrackSyncContext())
                {
                    if (!ctx.Database.CanConnect())
                    {
                        MessageBox.Show("Ошибка: Не удалось подключиться к базе данных!", "Ошибка");
                        return null;
                    }

                    if (string.IsNullOrWhiteSpace(searchText))
                    {
                        return ctx.Ingredients.ToList();
                    }

                    var searchLower = searchText.ToLower();
                    return ctx.Ingredients
                        .Where(i => i.Title.ToLower().Contains(searchLower))
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при поиске ингредиентов:\n{ex.Message}", "Ошибка");
                return null;
            }
        }

        /// <summary>
        /// Получить все рецепты из базы данных
        /// </summary>
        public static List<Recipe> GetAllRecipes()
        {
            try
            {
                using (var ctx = new TrackSyncContext())
                {
                    if (!ctx.Database.CanConnect())
                    {
                        MessageBox.Show("Ошибка: Не удалось подключиться к базе данных! Проверьте настройки подключения.", "Ошибка");
                        return null;
                    }

                    return ctx.Recipes
                        .Include(r => r.IngredientsRecipes)
                        .ThenInclude(ir => ir.Ingredient)
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при подключении к базе данных:\n{ex.Message}", "Ошибка");
                return null;
            }
        }

        /// <summary>
        /// Поиск рецептов по названию
        /// </summary>
        public static List<Recipe> SearchRecipes(string searchText)
        {
            try
            {
                using (var ctx = new TrackSyncContext())
                {
                    if (!ctx.Database.CanConnect())
                    {
                        MessageBox.Show("Ошибка: Не удалось подключиться к базе данных!", "Ошибка");
                        return null;
                    }

                    if (string.IsNullOrWhiteSpace(searchText))
                    {
                        return ctx.Recipes
                            .Include(r => r.IngredientsRecipes)
                            .ThenInclude(ir => ir.Ingredient)
                            .ToList();
                    }

                    var searchLower = searchText.ToLower();
                    return ctx.Recipes
                        .Include(r => r.IngredientsRecipes)
                        .ThenInclude(ir => ir.Ingredient)
                        .Where(r => r.Title.ToLower().Contains(searchLower))
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при поиске рецептов:\n{ex.Message}", "Ошибка");
                return null;
            }
        }

        /// <summary>
        /// Получить рецепт по ID
        /// </summary>
        public static Recipe GetRecipeById(int id)
        {
            try
            {
                using (var ctx = new TrackSyncContext())
                {
                    if (!ctx.Database.CanConnect())
                    {
                        MessageBox.Show("Ошибка: Не удалось подключиться к базе данных!", "Ошибка");
                        return null;
                    }

                    var recipe = ctx.Recipes
                        .Include(r => r.IngredientsRecipes)
                        .ThenInclude(ir => ir.Ingredient)
                        .FirstOrDefault(r => r.RecipeId == id);

                    if (recipe == null)
                    {
                        MessageBox.Show($"Рецепт с ID {id} не найден", "Ошибка");
                        return null;
                    }

                    return recipe;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при получении рецепта:\n{ex.Message}", "Ошибка");
                return null;
            }
        }

        /// <summary>
        /// Расчёт калорийности рецепта
        /// </summary>
        public static int CalculateRecipeCalories(Recipe recipe)
        {
            if (recipe?.IngredientsRecipes == null)
                return 0;

            int totalCalories = 0;
            foreach (var ir in recipe.IngredientsRecipes)
            {
                if (ir.Ingredient != null)
                {
                    // Калории на 100г * вес в граммах / 100
                    totalCalories += (int)(ir.Ingredient.Calories * ir.Weight / 100.0);
                }
            }
            return totalCalories;
        }

        /// <summary>
        /// Расчёт общего веса рецепта
        /// </summary>
        public static int CalculateRecipeWeight(Recipe recipe)
        {
            if (recipe?.IngredientsRecipes == null)
                return 0;

            return recipe.IngredientsRecipes.Sum(ir => ir.Weight);
        }

        /// <summary>
        /// Получить комнаты, в которых участвует пользователь
        /// </summary>
        public static List<HikeRoom> GetUserRooms(int userId)
        {
            try
            {
                using var ctx = new TrackSyncContext();

                // Сначала получаем список ID комнат пользователя
                var roomIds = ctx.Participants
                    .Where(p => p.UserId == userId)
                    .Select(p => p.HikeRoomId)
                    .ToList();

                // Затем загружаем комнаты с включением связанных сущностей
                return ctx.HikeRooms
                    .Where(r => roomIds.Contains(r.HikeRoomId))
                    .Include(r => r.Status)
                    .Include(r => r.Participants)
                    .OrderByDescending(r => r.StartDate)
                    .ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при получении комнат пользователя:\n{ex.Message}", "Ошибка");
                return null;
            }
        }

        public static List<HikeRoom> GetAvailableRooms(int userId)
        {
            try
            {
                using (var ctx = new TrackSyncContext())
                {
                    var availableRooms = ctx.HikeRooms
                        .AsNoTracking() // Отключаем трекинг, так как данные только для чтения
                        .Include(r => r.Status) // Загружаем статус (если нужен для отображения)
                        .Include(r => r.Participants) // Не загружаем .Include(r => r.Participants), если не нужны данные участников
                        .Where(r => r.StatusId == 1) // Активные комнаты
                        .Where(r => !ctx.Participants.Any(p => p.UserId == userId && p.HikeRoomId == r.HikeRoomId)) // Пользователь не участник
                        .ToList();

                    return availableRooms;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при получении доступных комнат:\n{ex.Message}", "Ошибка");
                return new List<HikeRoom>(); // Возвращаем пустой список вместо null для безопасности
            }
        }

        /// <summary>
        /// Получить всех участников комнаты по её ID
        /// </summary>
        public static List<Participant> GetParticipantsByRoomId(int roomId)
        {
            try
            {
                using (var ctx = new TrackSyncContext())
                {
                    if (!ctx.Database.CanConnect())
                    {
                        MessageBox.Show("Ошибка: Не удалось подключиться к базе данных!", "Ошибка");
                        return null;
                    }

                    return ctx.Participants
                        .Include(p => p.User) // Загружаем связанного пользователя
                        .Where(p => p.HikeRoomId == roomId) // Сортируем по email
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при получении участников комнаты:\n{ex.Message}", "Ошибка");
                return null;
            }
        }

        /// <summary>
        /// Получить снаряжение участника комнаты
        /// </summary>
        public static List<HikeRoomsEquipment> GetEquipmentByParticipantId(int participantId)
        {
            try
            {
                using (var ctx = new TrackSyncContext())
                {
                    if (!ctx.Database.CanConnect())
                    {
                        MessageBox.Show("Ошибка: Не удалось подключиться к базе данных!", "Ошибка");
                        return null;
                    }

                    return ctx.HikeRoomsEquipments
                        .Include(hre => hre.Equipment)
                        .Where(hre => hre.ParticipantId == participantId)
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при получении снаряжения участника:\n{ex.Message}", "Ошибка");
                return null;
            }
        }

        /// <summary>
        /// Получить всех участников комнаты с их снаряжением
        /// </summary>
        /// <summary>
        /// Получить всех участников комнаты с их снаряжением
        /// </summary>
        public static List<Participant> GetParticipantsWithEquipment(int roomId)
        {
            try
            {
                using (var ctx = new TrackSyncContext())
                {
                    if (!ctx.Database.CanConnect())
                    {
                        MessageBox.Show("Ошибка: Не удалось подключиться к базе данных!", "Ошибка");
                        return null;
                    }

                    // Загружаем участников комнаты ВМЕСТЕ со всем их снаряжением из таблицы HikeRoomsEquipments
                    return ctx.Participants
                        .Include(p => p.User)
                        .Include(p => p.HikeRoomsEquipments)          
                            .ThenInclude(hre => hre.Equipment)        
                        .Where(p => p.HikeRoomId == roomId)
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при получении участников комнаты:\n{ex.Message}", "Ошибка");
                return null;
            }
        }
        /// <summary>
        /// Получить ингредиент по ID
        /// </summary>
        public static Ingredient GetIngredientById(int id)
        {
            try
            {
                using (var ctx = new TrackSyncContext())
                {
                    if (!ctx.Database.CanConnect())
                    {
                        MessageBox.Show("Ошибка: Не удалось подключиться к базе данных!", "Ошибка");
                        return null;
                    }

                    var ingredient = ctx.Ingredients.FirstOrDefault(i => i.IngredientId == id);

                    if (ingredient == null)
                    {
                        MessageBox.Show($"Ингредиент с ID {id} не найден", "Ошибка");
                        return null;
                    }

                    return ingredient;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при получении ингредиента:\n{ex.Message}", "Ошибка");
                return null;
            }
        }
        /// <summary>
        /// Получить все ингредиенты для конкретного пользователя (системные + свои)
        /// </summary>
        public static List<Ingredient> GetAllIngredientsForUser(int userId)
        {
            try
            {
                using (var ctx = new TrackSyncContext())
                {
                    if (!ctx.Database.CanConnect())
                    {
                        MessageBox.Show("Ошибка: Не удалось подключиться к базе данных! Проверьте настройки подключения.", "Ошибка");
                        return null;
                    }

                    return ctx.Ingredients
                        .Where(i => i.CreatorUserId == null || i.CreatorUserId == userId)
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при подключении к базе данных:\n{ex.Message}", "Ошибка");
                return null;
            }
        }

        /// <summary>
        /// Получить только ингредиенты, созданные пользователем
        /// </summary>
        public static List<Ingredient> GetUserIngredients(int userId)
        {
            try
            {
                using (var ctx = new TrackSyncContext())
                {
                    if (!ctx.Database.CanConnect())
                    {
                        MessageBox.Show("Ошибка: Не удалось подключиться к базе данных! Проверьте настройки подключения.", "Ошибка");
                        return null;
                    }

                    return ctx.Ingredients
                        .Where(i => i.CreatorUserId == userId)
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при подключении к базе данных:\n{ex.Message}", "Ошибка");
                return null;
            }
        }
        /// <summary>
        /// Получить все рецепты для конкретного пользователя (системные + свои)
        /// </summary>
        public static List<Recipe> GetAllRecipesForUser(int userId)
        {
            try
            {
                using (var ctx = new TrackSyncContext())
                {
                    if (!ctx.Database.CanConnect())
                    {
                        MessageBox.Show("Ошибка: Не удалось подключиться к базе данных! Проверьте настройки подключения.", "Ошибка");
                        return null;
                    }

                    return ctx.Recipes
                        .Include(r => r.IngredientsRecipes)
                        .ThenInclude(ir => ir.Ingredient)
                        .Where(r => r.CreatorUserId == null || r.CreatorUserId == userId)
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при подключении к базе данных:\n{ex.Message}", "Ошибка");
                return null;
            }
        }

        /// <summary>
        /// Получить только рецепты, созданные пользователем
        /// </summary>
        public static List<Recipe> GetUserRecipes(int userId)
        {
            try
            {
                using (var ctx = new TrackSyncContext())
                {
                    if (!ctx.Database.CanConnect())
                    {
                        MessageBox.Show("Ошибка: Не удалось подключиться к базе данных! Проверьте настройки подключения.", "Ошибка");
                        return null;
                    }

                    return ctx.Recipes
                        .Include(r => r.IngredientsRecipes)
                        .ThenInclude(ir => ir.Ingredient)
                        .Where(r => r.CreatorUserId == userId)
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при подключении к базе данных:\n{ex.Message}", "Ошибка");
                return null;
            }
        }
        /// <summary>
        /// Получить рецепт по ID с полной загрузкой ингредиентов
        /// </summary>
        public static Recipe GetRecipeByIdFull(int id)
        {
            try
            {
                using (var ctx = new TrackSyncContext())
                {
                    if (!ctx.Database.CanConnect())
                    {
                        MessageBox.Show("Ошибка: Не удалось подключиться к базе данных!", "Ошибка");
                        return null;
                    }

                    var recipe = ctx.Recipes
                        .Include(r => r.IngredientsRecipes)
                        .ThenInclude(ir => ir.Ingredient)
                        .FirstOrDefault(r => r.RecipeId == id);

                    if (recipe == null)
                    {
                        MessageBox.Show($"Рецепт с ID {id} не найден", "Ошибка");
                        return null;
                    }

                    return recipe;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при получении рецепта:\n{ex.Message}", "Ошибка");
                return null;
            }
        }

        /// <summary>
        /// Получить комнату похода по её ID с загрузкой связанных сущностей
        /// </summary>
        public static HikeRoom GetHikeRoomById(int roomId)
        {
            try
            {
                using (var ctx = new TrackSyncContext())
                {
                    if (!ctx.Database.CanConnect())
                    {
                        MessageBox.Show("Ошибка: Не удалось подключиться к базе данных!", "Ошибка");
                        return null;
                    }

                    return ctx.HikeRooms
                        .Include(hr => hr.Status)
                        .Include(hr => hr.Participants)
                            .ThenInclude(p => p.User)
                        .FirstOrDefault(hr => hr.HikeRoomId == roomId);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при получении данных комнаты:\n{ex.Message}", "Ошибка");
                return null;
            }
        }

        /// <summary>
        /// Получить все приглашения пользователя
        /// </summary>
        public static List<Invitation> GetUserInvitations(int userId)
        {
            try
            {
                using (var ctx = new TrackSyncContext())
                {
                    if (!ctx.Database.CanConnect())
                    {
                        MessageBox.Show("Ошибка: Не удалось подключиться к базе данных!", "Ошибка");
                        return null;
                    }

                    return ctx.Invitations
                        .Include(i => i.HikeRoom)
                            .ThenInclude(hr => hr.Status)
                        .Where(i => i.UserId == userId)
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при получении приглашений:\n{ex.Message}", "Ошибка");
                return null;
            }
        }

        /// <summary>
        /// Принять приглашение - добавить пользователя в комнату как участника
        /// </summary>
        public static bool AcceptInvitation(int invitationId, int userId)
        {
            try
            {
                using (var ctx = new TrackSyncContext())
                {
                    if (!ctx.Database.CanConnect())
                    {
                        MessageBox.Show("Ошибка: Не удалось подключиться к базе данных!", "Ошибка");
                        return false;
                    }

                    // Находим приглашение
                    var invitation = ctx.Invitations
                        .Include(i => i.HikeRoom)
                        .FirstOrDefault(i => i.InvitationId == invitationId && i.UserId == userId);

                    if (invitation == null)
                    {
                        MessageBox.Show("Приглашение не найдено!", "Ошибка");
                        return false;
                    }

                    // Проверяем, не является ли пользователь уже участником комнаты
                    if (NotificationsPanel.IsUserAlreadyParticipant(userId, invitation.HikeRoomId))
                    {
                        ctx.Invitations.Remove(invitation);
                        ctx.SaveChanges();
                        return true; // Уже участник, считаем успехом
                    }

                    // Используем безопасное добавление
                    var success = NotificationsPanel.AddParticipantSafely(
                        userId: userId,
                        roomId: invitation.HikeRoomId,
                        editMealPlan: false,
                        editTrack: false,
                        editEquipment: false
                    );

                    if (success)
                    {
                        // Удаляем приглашение
                        ctx.Invitations.Remove(invitation);

                        // Также удаляем все заявки этого пользователя в эту комнату
                        var bidsToRemove = ctx.Bids
                            .Where(b => b.UserId == userId && b.HikeRoomId == invitation.HikeRoomId)
                            .ToList();

                        if (bidsToRemove.Any())
                        {
                            ctx.Bids.RemoveRange(bidsToRemove);
                        }

                        ctx.SaveChanges();
                    }

                    return success;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при принятии приглашения:\n{ex.Message}", "Ошибка");
                return false;
            }
        }

        /// <summary>
        /// Отклонить приглашение - удалить из базы данных
        /// </summary>
        public static bool RejectInvitation(int invitationId, int userId)
        {
            try
            {
                using (var ctx = new TrackSyncContext())
                {
                    if (!ctx.Database.CanConnect())
                    {
                        MessageBox.Show("Ошибка: Не удалось подключиться к базе данных!", "Ошибка");
                        return false;
                    }

                    var invitation = ctx.Invitations
                        .FirstOrDefault(i => i.InvitationId == invitationId && i.UserId == userId);

                    if (invitation == null)
                    {
                        MessageBox.Show("Приглашение не найдено!", "Ошибка");
                        return false;
                    }

                    ctx.Invitations.Remove(invitation);
                    ctx.SaveChanges();

                    MessageBox.Show("Приглашение отклонено", "Информация",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                    return true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при отклонении приглашения:\n{ex.Message}", "Ошибка");
                return false;
            }
        }

        /// <summary>
        /// Получить количество непрочитанных приглашений пользователя
        /// </summary>
        public static int GetInvitationsCount(int userId)
        {
            try
            {
                using (var ctx = new TrackSyncContext())
                {
                    if (!ctx.Database.CanConnect())
                    {
                        return 0;
                    }

                    return ctx.Invitations
                        .Count(i => i.UserId == userId);
                }
            }
            catch
            {
                return 0;
            }
        }
        /// <summary>
        /// Получить всех пользователей для приглашения в комнату (исключая текущего пользователя, участников, приглашенных и отправивших заявки)
        /// </summary>
        public static List<User> GetUsersForInvitation(int currentRoomId, int currentUserId)
        {
            try
            {
                using (var ctx = new TrackSyncContext())
                {
                    if (!ctx.Database.CanConnect())
                    {
                        MessageBox.Show("Ошибка: Не удалось подключиться к базе данных!", "Ошибка");
                        return null;
                    }

                    // Получаем все ID пользователей, которых нужно исключить:

                    // 1. Участники текущей комнаты
                    var participantUserIds = ctx.Participants
                        .Where(p => p.HikeRoomId == currentRoomId)
                        .Select(p => p.UserId)
                        .ToList();

                    // 2. Пользователи с приглашениями в эту комнату
                    var invitedUserIds = ctx.Invitations
                        .Where(i => i.HikeRoomId == currentRoomId)
                        .Select(i => i.UserId)
                        .ToList();

                    // 3. Пользователи с заявками (bids) в эту комнату
                    var bidUserIds = ctx.Bids
                        .Where(b => b.HikeRoomId == currentRoomId)
                        .Select(b => b.UserId)
                        .ToList();

                    // 4. Текущий пользователь
                    var currentUserIds = new List<int> { currentUserId };

                    // Объединяем все ID для исключения
                    var excludedUserIds = participantUserIds
                        .Concat(invitedUserIds)
                        .Concat(bidUserIds)
                        .Concat(currentUserIds)
                        .Distinct()
                        .ToList();

                    // Получаем всех пользователей, кроме исключенных
                    var usersList = ctx.Users
                        .Where(u => !excludedUserIds.Contains(u.UserId))
                        .ToList();

                    return usersList;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при получении списка пользователей для приглашения:\n{ex.Message}", "Ошибка");
                return null;
            }
        }

        /// <summary>
        /// Поиск пользователей для приглашения по email
        /// </summary>
        public static List<User> SearchUsersForInvitation(string searchText, int currentRoomId, int currentUserId)
        {
            try
            {
                using (var ctx = new TrackSyncContext())
                {
                    if (!ctx.Database.CanConnect())
                    {
                        MessageBox.Show("Ошибка: Не удалось подключиться к базе данных!", "Ошибка");
                        return null;
                    }

                    // Получаем исключенные ID (так же как в методе выше)
                    var participantUserIds = ctx.Participants
                        .Where(p => p.HikeRoomId == currentRoomId)
                        .Select(p => p.UserId)
                        .ToList();

                    var invitedUserIds = ctx.Invitations
                        .Where(i => i.HikeRoomId == currentRoomId)
                        .Select(i => i.UserId)
                        .ToList();

                    var bidUserIds = ctx.Bids
                        .Where(b => b.HikeRoomId == currentRoomId)
                        .Select(b => b.UserId)
                        .ToList();

                    var currentUserIds = new List<int> { currentUserId };

                    var excludedUserIds = participantUserIds
                        .Concat(invitedUserIds)
                        .Concat(bidUserIds)
                        .Concat(currentUserIds)
                        .Distinct()
                        .ToList();

                    // Поиск по имени или email
                    var searchLower = searchText.ToLower();
                    var usersList = ctx.Users
                        .Where(u => !excludedUserIds.Contains(u.UserId) &&
                                    (u.Email != null && u.Email.ToLower().Contains(searchLower)))
                        .ToList();

                    return usersList;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при поиске пользователей для приглашения:\n{ex.Message}", "Ошибка");
                return null;
            }
        }

        /// <summary>
        /// Отправить приглашения выбранным пользователям в комнату
        /// </summary>
        public static bool SendInvitations(List<int> userIds, int roomId, out int invitationsCreated, out int invitationsSkipped)
        {
            invitationsCreated = 0;
            invitationsSkipped = 0;

            try
            {
                using (var ctx = new TrackSyncContext())
                {
                    if (!ctx.Database.CanConnect())
                    {
                        MessageBox.Show("Ошибка: Не удалось подключиться к базе данных!", "Ошибка");
                        return false;
                    }

                    foreach (var userId in userIds)
                    {
                        // Проверяем, можно ли отправить приглашение
                        bool canInvite = !ctx.Participants.Any(p => p.HikeRoomId == roomId && p.UserId == userId) &&
                                        !ctx.Invitations.Any(i => i.HikeRoomId == roomId && i.UserId == userId) &&
                                        !ctx.Bids.Any(b => b.HikeRoomId == roomId && b.UserId == userId);

                        if (canInvite)
                        {
                            var invitation = new Invitation
                            {
                                UserId = userId,
                                HikeRoomId = roomId
                            };
                            ctx.Invitations.Add(invitation);
                            invitationsCreated++;
                        }
                        else
                        {
                            invitationsSkipped++;
                        }
                    }

                    if (invitationsCreated > 0)
                    {
                        ctx.SaveChanges();
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при отправке приглашений:\n{ex.Message}", "Ошибка");
                return false;
            }
        }

        /// <summary>
        /// Получение заявок в комнаты, где текущий пользователь является лидером
        /// </summary>
        public static List<Bid> GetUserBids(int userId)
        {
            using (var context = new TrackSyncContext())
            {
                // Находим комнаты, где пользователь является лидером
                var leaderRoomIds = context.HikeRooms
                    .Where(r => r.LeaderId == userId)
                    .Select(r => r.HikeRoomId)
                    .ToList();

                // Загружаем заявки в эти комнаты с навигационными свойствами
                return context.Bids
                    .Include(b => b.HikeRoom)
                    .Include(b => b.User)
                    .Where(b => leaderRoomIds.Contains(b.HikeRoomId))
                    .ToList();
            }
        }

        /// <summary>
        /// Принять заявку пользователя — добавить его в участники похода
        /// </summary>
        public static bool AcceptBid(int bidId, int currentUserId)
        {
            using (var context = new TrackSyncContext())
            {
                var bid = context.Bids
                    .Include(b => b.HikeRoom)
                    .FirstOrDefault(b => b.BidId == bidId);

                if (bid == null || bid.HikeRoom.LeaderId != currentUserId)
                    return false;

                // Проверяем, не является ли пользователь уже участником комнаты
                if (NotificationsPanel.IsUserAlreadyParticipant(bid.UserId, bid.HikeRoomId))
                {
                    context.Bids.Remove(bid);
                    context.SaveChanges();
                    return true; // Уже участник, считаем успехом
                }

                // Используем безопасное добавление
                var success = NotificationsPanel.AddParticipantSafely(
                    userId: bid.UserId,
                    roomId: bid.HikeRoomId,
                    editMealPlan: false,
                    editTrack: false,
                    editEquipment: false
                );

                if (success)
                {
                    // Удаляем заявку
                    context.Bids.Remove(bid);

                    // Также удаляем все приглашения этого пользователя в эту комнату
                    var invitationsToRemove = context.Invitations
                        .Where(i => i.UserId == bid.UserId && i.HikeRoomId == bid.HikeRoomId)
                        .ToList();

                    if (invitationsToRemove.Any())
                    {
                        context.Invitations.RemoveRange(invitationsToRemove);
                    }

                    context.SaveChanges();
                }

                return success;
            }
        }

        /// <summary>
        /// Отклонить заявку — просто удаляем её
        /// </summary>
        public static bool RejectBid(int bidId)
        {
            using (var context = new TrackSyncContext())
            {
                var bid = context.Bids.Find(bidId);
                if (bid == null)
                    return false;

                context.Bids.Remove(bid);

                try
                {
                    context.SaveChanges();
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Класс для хранения данных о дне раскладки
        /// </summary>
        public class MealPlanDay
        {
            public int DayNumber { get; set; }
            public DateOnly Date { get; set; }
            public List<MealPlanMeal> Meals { get; set; } = new List<MealPlanMeal>();
        }

        /// <summary>
        /// Класс для хранения данных о приёме пищи
        /// </summary>
        public class MealPlanMeal
        {
            public int TypeMealId { get; set; }
            public Recipe Recipe { get; set; }
            public List<RecipesParticipant> ResponsibleParticipants { get; set; } = new List<RecipesParticipant>();
        }

        /// <summary>
        /// Получить раскладку питания для комнаты
        /// </summary>
        public static List<MealPlanDay> GetMealPlanForRoom(int roomId)
        {
            try
            {
                using (var ctx = new TrackSyncContext())
                {
                    if (!ctx.Database.CanConnect())
                    {
                        MessageBox.Show("Ошибка: Не удалось подключиться к базе данных!", "Ошибка");
                        return null;
                    }

                    // Получаем комнату
                    var room = ctx.HikeRooms.Find(roomId);
                    if (room == null)
                        return new List<MealPlanDay>();

                    // Вычисляем количество дней
                    int totalDays = (room.EndDate.ToDateTime(TimeOnly.MinValue) -
                                    room.StartDate.ToDateTime(TimeOnly.MinValue)).Days + 1;

                    var result = new List<MealPlanDay>();

                    // Получаем всех участников комнаты
                    var participantIdsInRoom = ctx.Participants
                        .Where(p => p.HikeRoomId == roomId)
                        .Select(p => p.ParticipantId)
                        .ToList();

                    // Получаем все рецепты для комнаты
                    var allRecipeEntries = ctx.HikeRoomsRecipes
                        .Where(hrr => hrr.HikeRoomId == roomId)
                        .Include(hrr => hrr.Recipe)
                            .ThenInclude(r => r.IngredientsRecipes)
                                .ThenInclude(ir => ir.Ingredient)
                        .ToList()
                        .GroupBy(hrr => new { hrr.MealDate, hrr.TypeMealId })
                        .ToDictionary(
                            g => g.Key,
                            g => g.FirstOrDefault()
                        );

                    // Получаем всех ответственных для рецептов в этой комнате
                    var allResponsibleParticipants = new Dictionary<int, List<RecipesParticipant>>();

                    foreach (var recipeEntry in allRecipeEntries.Values)
                    {
                        if (recipeEntry?.RecipeId != null)
                        {
                            var responsible = ctx.RecipesParticipants
                                .Where(rp => rp.RecipeId == recipeEntry.RecipeId &&
                                           participantIdsInRoom.Contains(rp.ParticipantId))
                                .Include(rp => rp.Participant)
                                    .ThenInclude(p => p.User)
                                .ToList();

                            allResponsibleParticipants[recipeEntry.RecipeId] = responsible;
                        }
                    }

                    // Создаём дни
                    for (int i = 0; i < totalDays; i++)
                    {
                        var date = room.StartDate.AddDays(i);
                        var day = new MealPlanDay
                        {
                            DayNumber = i + 1,
                            Date = date
                        };

                        // Создаём приёмы пищи
                        var mealTypes = new[]
                        {
                    new { TypeMealId = 1, MealName = "Завтрак", Tag = $"breakfast_day{i + 1}" },
                    new { TypeMealId = 2, MealName = "Обед", Tag = $"lunch_day{i + 1}" },
                    new { TypeMealId = 4, MealName = "Перекус", Tag = $"snack_day{i + 1}" },
                    new { TypeMealId = 3, MealName = "Ужин", Tag = $"dinner_day{i + 1}" }
                };

                        foreach (var mealType in mealTypes)
                        {
                            var meal = new MealPlanMeal
                            {
                                TypeMealId = mealType.TypeMealId
                            };

                            // Ищем рецепт для этого приёма пищи в этот день
                            var key = new { MealDate = date, TypeMealId = mealType.TypeMealId };
                            if (allRecipeEntries.TryGetValue(key, out var recipeEntry) && recipeEntry != null)
                            {
                                meal.Recipe = recipeEntry.Recipe;

                                // Получаем ответственных для этого рецепта
                                if (allResponsibleParticipants.TryGetValue(recipeEntry.RecipeId, out var responsible))
                                {
                                    meal.ResponsibleParticipants = responsible;
                                }
                            }

                            day.Meals.Add(meal);
                        }

                        result.Add(day);
                    }

                    return result;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке раскладки питания:\n{ex.Message}", "Ошибка");
                return null;
            }
        }

        public static Track GetTrackForRoom(int roomId)
        {
            try
            {
                using (var ctx = new TrackSyncContext())
                {
                    return ctx.Tracks
                        .Include(t => t.Segments)
                            .ThenInclude(s => s.StartWaypoint)
                        .Include(t => t.Segments)
                            .ThenInclude(s => s.EndWaypoint)
                        .FirstOrDefault(t => t.HikeRoomId == roomId);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке маршрута: {ex.Message}");
                return null;
            }
        }
        public static List<Track> GetAllTracksForRoom(int roomId)
        {
            try
            {
                using (var ctx = new TrackSyncContext())
                {
                    return ctx.Tracks
                        .AsNoTracking()
                        .Where(t => t.HikeRoomId == roomId)
                        .Include(t => t.Participant)          // Загружаем участника
                            .ThenInclude(p => p.User)         // Внутри участника загружаем пользователя
                        .Include(t => t.Segments)
                            .ThenInclude(s => s.StartWaypoint)
                        .Include(t => t.Segments)
                            .ThenInclude(s => s.EndWaypoint)
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке маршрута: {ex.Message}");
                return null;
            }
        }
        public static int GetCurrentParticipantId(int userId, int roomId)
        {
            try
            {
                using (var ctx = new TrackSyncContext())
                {
                    // Отключаем отслеживание и ленивую загрузку для этого запроса
                    return ctx.Participants
                        .AsNoTracking()
                        .Where(p => p.UserId == userId && p.HikeRoomId == roomId)
                        .Select(p => p.ParticipantId) // Берем только ID, не загружая весь объект
                        .FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                // Логируем в консоль, чтобы не спамить месседжбоксами при частых вызовах
                Console.WriteLine($"Ошибка при поиске участника: {ex.Message}");
                return 0;
            }
        }


    }
}