using System.Collections.Generic;

namespace TrackSync.Interfaces
{
    public interface IRecipeCallback
    {
        void OnRecipeSelected(Recipe recipe, List<int> responsibleParticipantIds);

    }
}