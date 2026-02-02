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
    }
}