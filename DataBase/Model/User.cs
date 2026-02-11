using System;
using System.Collections.Generic;

namespace TrackSync.DataBase.Model
{
    public partial class User
    {
        public User()
        {
            Bids = new HashSet<Bid>();
            Equipment = new HashSet<Equipment>();
            Ingredients = new HashSet<Ingredient>();
            Invitations = new HashSet<Invitation>();
            Participants = new HashSet<Participant>();
            Recipes = new HashSet<Recipe>();
        }

        public User(string password, string name, string email, int weight, int height)
        {
            Password = BCrypt.Net.BCrypt.HashPassword(password);
            Name = name;
            Email = email;
            Weight = weight;
            Height = height;
        }
        public int UserId { get; set; }
        public string Password { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? Contacts { get; set; }
        public int Weight { get; set; }
        public int Height { get; set; }
        public string Name { get; set; } = null!;

        public virtual ICollection<Bid> Bids { get; set; }
        public virtual ICollection<Equipment> Equipment { get; set; }
        public virtual ICollection<HikeRoom> HikeRooms { get; set; }
        public virtual ICollection<Ingredient> Ingredients { get; set; }
        public virtual ICollection<Invitation> Invitations { get; set; }
        public virtual ICollection<Participant> Participants { get; set; }
        public virtual ICollection<Recipe> Recipes { get; set; }
    }
}
