using System;
using System.Collections.Generic;

namespace TrackSync
{
    public partial class Loginattempt
    {
        public int AttemptId { get; set; }
        public string IpAddress { get; set; } = null!;
        public string Email { get; set; } = null!;
        public DateTime AttemptTime { get; set; }
        public bool Success { get; set; }
    }
}
