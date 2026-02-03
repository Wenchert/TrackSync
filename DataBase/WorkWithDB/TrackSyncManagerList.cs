using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
//using TrackSync.DataBase.Model;

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

        /// <summary>
        /// Получить все доступные комнаты (публичные + приглашённые)
        /// </summary>
        public static List<HikeRoom> GetAvailableRooms(int userId)
        {
            try
            {
                using var ctx = new TrackSyncContext();

                // Получаем ID публичных комнат
                var publicRoomIds = ctx.HikeRooms
                    .Where(r => r.AccessType)
                    .Select(r => r.HikeRoomId)
                    .ToList();

                // Получаем ID приватных комнат, в которые приглашён пользователь
                var invitedRoomIds = ctx.Invitations
                    .Where(i => i.UserId == userId)
                    .Select(i => i.HikeRoomId)
                    .ToList();

                // Объединяем списки ID
                var allRoomIds = publicRoomIds.Union(invitedRoomIds).ToList();

                // Загружаем комнаты с включением связанных сущностей
                return ctx.HikeRooms
                    .Where(r => allRoomIds.Contains(r.HikeRoomId))
                    .Include(r => r.Status)
                    .Include(r => r.Participants)
                    .OrderByDescending(r => r.StartDate)
                    .ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при получении доступных комнат:\n{ex.Message}", "Ошибка");
                return null;
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
                    var existingParticipant = ctx.Participants
                        .FirstOrDefault(p => p.UserId == userId && p.HikeRoomId == invitation.HikeRoomId);

                    if (existingParticipant != null)
                    {
                        MessageBox.Show("Вы уже являетесь участником этой комнаты!", "Ошибка");
                        // Удаляем приглашение, так как оно больше не нужно
                        ctx.Invitations.Remove(invitation);
                        ctx.SaveChanges();
                        return false;
                    }

                    // Создаем нового участника с базовыми правами (без прав на редактирование)
                    var participant = new Participant(
                        userId: userId,
                        hikeRoomId: invitation.HikeRoomId,
                        editMealPlan: false,
                        editTrack: false,
                        editEquipment: false
                    );

                    ctx.Participants.Add(participant);

                    // Удаляем приглашение
                    ctx.Invitations.Remove(invitation);

                    ctx.SaveChanges();

                    MessageBox.Show($"Вы присоединились к походу \"{invitation.HikeRoom.Title}\"!",
                                  "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    return true;
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
                                   (u.Name.ToLower().Contains(searchLower) ||
                                    (u.Name != null && u.Name.ToLower().Contains(searchLower)) ||
                                    (u.Contacts != null && u.Contacts.ToLower().Contains(searchLower))))
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
    }
}