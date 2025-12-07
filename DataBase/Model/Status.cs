using System;
using System.Collections.Generic;

namespace TrackSync.DataBase.Model
{
    public partial class Status
    {
        public Status()
        {
            HikeRooms = new HashSet<HikeRoom>();
        }

        public int StatusId { get; set; }
        public string Title { get; set; } = null!;

        public virtual ICollection<HikeRoom> HikeRooms { get; set; }
    }
}
