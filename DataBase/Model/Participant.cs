using System;
using System.Collections.Generic;

namespace TrackSync
{
    public partial class Participant
    {
        public Participant()
        {
            HikeRoomsEquipments = new HashSet<HikeRoomsEquipment>();
            RecipesParticipants = new HashSet<RecipesParticipant>();
        }
        public Participant(int userId, int hikeRoomId, bool editMealPlan, bool editTrack, bool editEquipment)
        {
            UserId = userId;
            HikeRoomId = hikeRoomId;
            EditMealPlan = editMealPlan;
            EditTrack = editTrack;
            EditEquipment = editEquipment;
        }
        public int ParticipantId { get; set; }
        public int UserId { get; set; }
        public int HikeRoomId { get; set; }
        public bool? EditMealPlan { get; set; }
        public bool? EditTrack { get; set; }
        public bool? EditEquipment { get; set; }

        public virtual HikeRoom HikeRoom { get; set; } = null!;
        public virtual User User { get; set; } = null!;
        public virtual ICollection<HikeRoomsEquipment> HikeRoomsEquipments { get; set; }
        public virtual ICollection<RecipesParticipant> RecipesParticipants { get; set; }
        public virtual ICollection<Waypoint> Waypoints { get; set; }
    }
}
