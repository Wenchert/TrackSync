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
        public Ingredient(string title, int calories, int fats, int proteins, int carbs, int price, int? creatorUserId = null, string? recommendedWeight = null)
        {
            Title = title;
            Calories = calories;
            Fats = fats;
            Proteins = proteins;
            Carbs = carbs;
            Price = price;
            CreatorUserId = creatorUserId;
            RecommendedWeight = recommendedWeight;
        }
        public int IngredientId { get; set; }
        public string Title { get; set; } = null!;
        public int Calories { get; set; }
        public int Fats { get; set; }
        public int Proteins { get; set; }
        public int Carbs { get; set; }
        public int Price { get; set; }
        public string? RecommendedWeight { get; set; }
        public int? CreatorUserId { get; set; }

        public virtual User? CreatorUser { get; set; }
        public virtual ICollection<IngredientsRecipe> IngredientsRecipes { get; set; }
    }
}
