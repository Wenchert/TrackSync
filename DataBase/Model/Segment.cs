using System;
using System.Collections.Generic;

namespace TrackSync.DataBase.Model
{
    public partial class Segment
    {
    public int SegmentId { get; set; }
    public int StartWaypointId { get; set; }
    public int EndWaypointId { get; set; }
    public int TrackId { get; set; }
    public int Distance { get; set; }

    public virtual Waypoint EndWaypoint { get; set; } = null!;
    public virtual Waypoint StartWaypoint { get; set; } = null!;
    public virtual Track Track { get; set; } = null!;
    }
}
