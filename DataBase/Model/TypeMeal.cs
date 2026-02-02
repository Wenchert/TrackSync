using System;
using System.Collections.Generic;

namespace TrackSync
{
    public partial class TypeMeal
    {
        public TypeMeal()
        {
            HikeRoomsRecipes = new HashSet<HikeRoomsRecipe>();
        }

        public int TypeMealId { get; set; }
        public string Title { get; set; } = null!;

        public virtual ICollection<HikeRoomsRecipe> HikeRoomsRecipes { get; set; }
    }
}
