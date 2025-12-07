using System;
using System.Collections.Generic;

namespace TrackSync.DataBase.Model
{
    public partial class User
    {
        public User()
        {
            Bids = new HashSet<Bid>();
            Invitations = new HashSet<Invitation>();
            Participants = new HashSet<Participant>();
        }

        public User(string password, string name, int weight, int height)
        {
            Password = password;
            Name = name;
            Weight = weight;
            Height = height;
        }

        public int UserId { get; set; }
        public string Password { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? Contacts { get; set; }
        public int Weight { get; set; }
        public int Height { get; set; }

        public virtual ICollection<Bid> Bids { get; set; }
        public virtual ICollection<Invitation> Invitations { get; set; }
        public virtual ICollection<Participant> Participants { get; set; }
    }
}
