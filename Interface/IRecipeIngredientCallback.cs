using TrackSync.DataBase.Model;

namespace TrackSync.Interface
{
    public interface IRecipeIngredientCallback
    {
        void OnIngredientsSelected(List<Ingredient> ingredients);
    }
}