using System;
using System.Collections.Generic;

namespace TrackSync.DataBase.Model
{
    public partial class RecipesParticipant
    {
        public int RecipeParticipantId { get; set; }
        public int RecipeId { get; set; }
        public int ParticipantId { get; set; }

        public virtual Participant Participant { get; set; } = null!;
        public virtual Recipe Recipe { get; set; } = null!;
    }
}
