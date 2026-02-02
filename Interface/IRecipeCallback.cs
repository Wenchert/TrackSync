using System.Collections.Generic;

namespace TrackSync.Interface
{
    public interface IRecipeCallback
    {
        void OnRecipesSelected(List<Recipe> recipes);
    }
}