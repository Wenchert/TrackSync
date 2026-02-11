using System.Collections.Generic;
using TrackSync.DataBase.Model;

namespace TrackSync.Interfaces
{
    public interface IRecipeCallback
    {
        void OnRecipeSelected(Recipe recipe, List<int> responsibleParticipantIds);

    }
}