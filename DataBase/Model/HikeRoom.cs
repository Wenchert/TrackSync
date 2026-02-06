using System;
using System.Collections.Generic;

namespace TrackSync
{
    public partial class HikeRoom
    {
        public HikeRoom()
        {
            Bids = new HashSet<Bid>();
            HikeRoomsEquipments = new HashSet<HikeRoomsEquipment>();
            HikeRoomsRecipes = new HashSet<HikeRoomsRecipe>();
            Invitations = new HashSet<Invitation>();
            Participants = new HashSet<Participant>();
            Tracks = new HashSet<Track>();
        }
        public HikeRoom(int statusId, bool accessType, string title, DateOnly startDate, DateOnly endDate, int leaderId, string? password = null, string? description = null)
        {
            StatusId = statusId;
            AccessType = accessType;
            Title = title;
            Password = password;
            Description = description;
            StartDate = startDate;
            EndDate = endDate;
            LeaderId = leaderId;
        }
        public int HikeRoomId { get; set; }
        public int StatusId { get; set; }
        public bool AccessType { get; set; }
        public string Title { get; set; } = null!;
        public string? Password { get; set; }
        public string? Description { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public int? LeaderId { get; set; }

        public virtual User? Leader { get; set; }
        public virtual Status Status { get; set; } = null!;
        public virtual ICollection<Bid> Bids { get; set; }
        public virtual ICollection<HikeRoomsEquipment> HikeRoomsEquipments { get; set; }
        public virtual ICollection<HikeRoomsRecipe> HikeRoomsRecipes { get; set; }
        public virtual ICollection<Invitation> Invitations { get; set; }
        public virtual ICollection<Participant> Participants { get; set; }
        public virtual ICollection<Track> Tracks { get; set; }
    }
}
