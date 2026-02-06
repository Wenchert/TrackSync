using System;
using System.Collections.Generic;

namespace TrackSync
{
    public partial class Track
    {
        public Track()
        {
            Segments = new HashSet<Segment>();
        }

        public int TrackId { get; set; }
        public int HikeRoomId { get; set; }
        public string Title { get; set; } = null!;
        public string Color { get; set; } = null!;

        public virtual HikeRoom HikeRoom { get; set; } = null!;
        public virtual ICollection<Segment> Segments { get; set; }
    }
}
