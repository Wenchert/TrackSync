using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Windows;
using TrackSync.Controls;
using TrackSync.DataBase.Model;
using TrackSync.Pages;


namespace TrackSync.DataBase.WorkWithDB
{
    public static class TrackSyncManagerList
    {
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

        public static int CalculateRecipeCalories(Recipe recipe)
        {
            if (recipe?.IngredientsRecipes == null)
                return 0;

            int totalCalories = 0;
            foreach (var ir in recipe.IngredientsRecipes)
            {
                if (ir.Ingredient != null)
                {
                    totalCalories += (int)(ir.Ingredient.Calories * ir.Weight / 100.0);
                }
            }
            return totalCalories;
        }

        public static int CalculateRecipeWeight(Recipe recipe)
        {
            if (recipe?.IngredientsRecipes == null)
                return 0;

            return recipe.IngredientsRecipes.Sum(ir => ir.Weight);
        }

        public static List<HikeRoom> GetUserRooms(int userId)
        {
            try
            {
                using var ctx = new TrackSyncContext();

                var roomIds = ctx.Participants
                    .Where(p => p.UserId == userId)
                    .Select(p => p.HikeRoomId)
                    .ToList();

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
                        .AsNoTracking() 
                        .Include(r => r.Leader) 
                        .Include(r => r.Participants) 
                        .Where(r => r.StatusId == 1) 
                        .Where(r => !ctx.Participants.Any(p => p.UserId == userId && p.HikeRoomId == r.HikeRoomId)) 
                        .ToList();

                    return availableRooms;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при получении доступных комнат:\n{ex.Message}", "Ошибка");
                return new List<HikeRoom>(); 
            }
        }

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
                        .Include(p => p.User)
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

                    var invitation = ctx.Invitations
                        .Include(i => i.HikeRoom)
                        .FirstOrDefault(i => i.InvitationId == invitationId && i.UserId == userId);

                    if (invitation == null)
                    {
                        MessageBox.Show("Приглашение не найдено!", "Ошибка");
                        return false;
                    }

                    if (NotificationsPanel.IsUserAlreadyParticipant(userId, invitation.HikeRoomId))
                    {
                        ctx.Invitations.Remove(invitation);
                        ctx.SaveChanges();
                        return true; 
                    }

                    var success = NotificationsPanel.AddParticipantSafely(
                        userId: userId,
                        roomId: invitation.HikeRoomId,
                        editMealPlan: false,
                        editTrack: false,
                        editEquipment: false
                    );

                    if (success)
                    {
                        ctx.Invitations.Remove(invitation);

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

                    var usersList = ctx.Users
                        .Where(u => !excludedUserIds.Contains(u.UserId))
                        .ToList();

                    var totalUsers = ctx.Users.Count();

                    return usersList;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при получении списка пользователей для приглашения:\n{ex.Message}", "Ошибка");
                return null;
            }
        }

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

                    var searchLower = searchText.ToLower();
                    var usersList = ctx.Users
                        .Where(u => !excludedUserIds.Contains(u.UserId) &&
                                    (u.Email.ToLower().Contains(searchLower) ||
                                     u.Name.ToLower().Contains(searchLower)))
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

        public static List<Bid> GetUserBids(int userId)
        {
            using (var context = new TrackSyncContext())
            {
                var leaderRoomIds = context.HikeRooms
                    .Where(r => r.LeaderId == userId)
                    .Select(r => r.HikeRoomId)
                    .ToList();

                return context.Bids
                    .Include(b => b.HikeRoom)
                    .Include(b => b.User)
                    .Where(b => leaderRoomIds.Contains(b.HikeRoomId))
                    .ToList();
            }
        }

        public static bool AcceptBid(int bidId, int currentUserId)
        {
            using (var context = new TrackSyncContext())
            {
                var bid = context.Bids
                    .Include(b => b.HikeRoom)
                    .FirstOrDefault(b => b.BidId == bidId);

                if (bid == null || bid.HikeRoom.LeaderId != currentUserId)
                    return false;

                if (NotificationsPanel.IsUserAlreadyParticipant(bid.UserId, bid.HikeRoomId))
                {
                    context.Bids.Remove(bid);
                    context.SaveChanges();
                    return true; 
                }

                var success = NotificationsPanel.AddParticipantSafely(
                    userId: bid.UserId,
                    roomId: bid.HikeRoomId,
                    editMealPlan: false,
                    editTrack: false,
                    editEquipment: false
                );

                if (success)
                {
                    context.Bids.Remove(bid);

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

        public class MealPlanDay
        {
            public int DayNumber { get; set; }
            public DateOnly Date { get; set; }
            public List<MealPlanMeal> Meals { get; set; } = new List<MealPlanMeal>();
        }

        public class MealPlanMeal
        {
            public int TypeMealId { get; set; }
            public Recipe Recipe { get; set; }
            public List<RecipesParticipant> ResponsibleParticipants { get; set; } = new List<RecipesParticipant>();
        }

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

                    var room = ctx.HikeRooms.Find(roomId);
                    if (room == null)
                        return new List<MealPlanDay>();

                    int totalDays = (room.EndDate.ToDateTime(TimeOnly.MinValue) -
                                    room.StartDate.ToDateTime(TimeOnly.MinValue)).Days + 1;

                    var result = new List<MealPlanDay>();

                    var participantIdsInRoom = ctx.Participants
                        .Where(p => p.HikeRoomId == roomId)
                        .Select(p => p.ParticipantId)
                        .ToList();

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

                    for (int i = 0; i < totalDays; i++)
                    {
                        var date = room.StartDate.AddDays(i);
                        var day = new MealPlanDay
                        {
                            DayNumber = i + 1,
                            Date = date
                        };

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

                            var key = new { MealDate = date, mealType.TypeMealId };
                            if (allRecipeEntries.TryGetValue(key, out var recipeEntry) && recipeEntry != null)
                            {
                                meal.Recipe = recipeEntry.Recipe;

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
                        .Include(t => t.Participant)         
                            .ThenInclude(p => p.User)         
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
                    return ctx.Participants
                        .AsNoTracking()
                        .Where(p => p.UserId == userId && p.HikeRoomId == roomId)
                        .Select(p => p.ParticipantId) 
                        .FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при поиске участника: {ex.Message}");
                return 0;
            }
        }


    }
}