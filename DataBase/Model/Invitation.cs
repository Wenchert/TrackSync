using System;
using System.Collections.Generic;

namespace TrackSync.DataBase.Model
{
    public partial class Invitation
    {
        public int InvitationId { get; set; }
        public int HikeRoomId { get; set; }
        public int UserId { get; set; }

        public virtual HikeRoom HikeRoom { get; set; } = null!;
        public virtual User User { get; set; } = null!;
    }
}
