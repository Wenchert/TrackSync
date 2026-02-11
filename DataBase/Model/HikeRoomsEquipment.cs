using System;
using System.Collections.Generic;

namespace TrackSync.DataBase.Model
{
    public partial class HikeRoomsEquipment
    {
        public int HikeRoomEquipmentId { get; set; }
        public int HikeRoomId { get; set; }
        public int EquipmentId { get; set; }
        public int ParticipantId { get; set; }
        public int Quantity { get; set; }

        public virtual Equipment Equipment { get; set; } = null!;
        public virtual HikeRoom HikeRoom { get; set; } = null!;
        public virtual Participant Participant { get; set; } = null!;
    }
}
