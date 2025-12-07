using System;
using System.Collections.Generic;

namespace TrackSync.DataBase.Model
{
    public partial class Ingredient
    {
        public Ingredient()
        {
            IngredientsRecipes = new HashSet<IngredientsRecipe>();
        }

        public int IngredientId { get; set; }
        public string Title { get; set; } = null!;
        public int Calories { get; set; }
        public int Fats { get; set; }
        public int Proteins { get; set; }
        public int Carbs { get; set; }
        public int Price { get; set; }
        public string? RecommendedWeight { get; set; }

        public virtual ICollection<IngredientsRecipe> IngredientsRecipes { get; set; }
    }
}
