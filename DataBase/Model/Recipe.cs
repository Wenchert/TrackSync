using System;
using System.Collections.Generic;

namespace TrackSync.DataBase.Model
{
    public partial class Recipe
    {
        public Recipe()
        {
            HikeRoomsRecipes = new HashSet<HikeRoomsRecipe>();
            IngredientsRecipes = new HashSet<IngredientsRecipe>();
            RecipesParticipants = new HashSet<RecipesParticipant>();
        }

        public int RecipeId { get; set; }
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public int? Time { get; set; }

        public virtual ICollection<HikeRoomsRecipe> HikeRoomsRecipes { get; set; }
        public virtual ICollection<IngredientsRecipe> IngredientsRecipes { get; set; }
        public virtual ICollection<RecipesParticipant> RecipesParticipants { get; set; }
    }
}
