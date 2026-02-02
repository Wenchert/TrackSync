using System;
using System.Collections.Generic;

namespace TrackSync
{
    public partial class HikeRoomsRecipe
    {
        public int HikeRoomRecipeId { get; set; }
        public int HikeRoomId { get; set; }
        public int RecipeId { get; set; }
        public int TypeMealId { get; set; }
        public DateOnly MealDate { get; set; }

        public virtual HikeRoom HikeRoom { get; set; } = null!;
        public virtual Recipe Recipe { get; set; } = null!;
        public virtual TypeMeal TypeMeal { get; set; } = null!;
    }
}
