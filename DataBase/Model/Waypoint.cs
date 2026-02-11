using System;
using System.Collections.Generic;

namespace TrackSync.DataBase.Model
{
    public partial class Waypoint
    {
        public Waypoint()
        {
            SegmentEndWaypoints = new HashSet<Segment>();
            SegmentStartWaypoints = new HashSet<Segment>();
        }

        public int WaypointId { get; set; }
        public string Title { get; set; } = null!;
        public decimal Longitude { get; set; }
        public decimal Latitude { get; set; }
        public int Height { get; set; }

        public virtual ICollection<Segment> SegmentEndWaypoints { get; set; }
        public virtual ICollection<Segment> SegmentStartWaypoints { get; set; }
    }
}
