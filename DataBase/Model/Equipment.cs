using System;
using System.Collections.Generic;

namespace TrackSync.DataBase.Model
{
    public partial class Equipment
    {
        public Equipment()
        {
            HikeRoomsEquipments = new HashSet<HikeRoomsEquipment>();
        }

        public int EquipmentId { get; set; }
        public string Title { get; set; } = null!;
        public int Weight { get; set; }
        public string? Description { get; set; }

        public virtual ICollection<HikeRoomsEquipment> HikeRoomsEquipments { get; set; }
    }
}
