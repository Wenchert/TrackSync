using System;
using System.Collections.Generic;

namespace TrackSync.DataBase.Model
{
    public partial class IngredientsRecipe
    {
        public int IngredientRecipeId { get; set; }
        public int IngredientId { get; set; }
        public int RecipeId { get; set; }
        public int Weight { get; set; }

        public virtual Ingredient Ingredient { get; set; } = null!;
        public virtual Recipe Recipe { get; set; } = null!;
    }
}
