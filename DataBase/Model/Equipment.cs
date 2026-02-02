using System;
using System.Collections.Generic;

namespace TrackSync
{
    public partial class Equipment
    {
        public Equipment()
        {
            HikeRoomsEquipments = new HashSet<HikeRoomsEquipment>();
        }
        public Equipment(string title, int weight, int? creatorUserId = null, string? description = null)
        {
            Title = title;
            Weight = weight;
            CreatorUserId = creatorUserId;
            Description = description;
        }

        public int EquipmentId { get; set; }
        public string Title { get; set; } = null!;
        public int Weight { get; set; }
        public string? Description { get; set; }
        public int? CreatorUserId { get; set; }

        public virtual User? CreatorUser { get; set; }
        public virtual ICollection<HikeRoomsEquipment> HikeRoomsEquipments { get; set; }
    }
}
