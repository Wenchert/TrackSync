using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace TrackSync.DataBase
{
    public partial class TrackSyncContext : DbContext
    {
        public TrackSyncContext()
        {
        }

        public TrackSyncContext(DbContextOptions<TrackSyncContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Bid> Bids { get; set; } = null!;
        public virtual DbSet<Equipment> Equipments { get; set; } = null!;
        public virtual DbSet<HikeRoom> HikeRooms { get; set; } = null!;
        public virtual DbSet<HikeRoomsEquipment> HikeRoomsEquipments { get; set; } = null!;
        public virtual DbSet<HikeRoomsRecipe> HikeRoomsRecipes { get; set; } = null!;
        public virtual DbSet<Ingredient> Ingredients { get; set; } = null!;
        public virtual DbSet<IngredientsRecipe> IngredientsRecipes { get; set; } = null!;
        public virtual DbSet<Invitation> Invitations { get; set; } = null!;
        public virtual DbSet<Loginattempt> Loginattempts { get; set; } = null!;
        public virtual DbSet<Participant> Participants { get; set; } = null!;
        public virtual DbSet<Recipe> Recipes { get; set; } = null!;
        public virtual DbSet<RecipesParticipant> RecipesParticipants { get; set; } = null!;
        public virtual DbSet<Segment> Segments { get; set; } = null!;
        public virtual DbSet<Status> Statuses { get; set; } = null!;
        public virtual DbSet<Track> Tracks { get; set; } = null!;
        public virtual DbSet<TypeMeal> TypeMeals { get; set; } = null!;
        public virtual DbSet<User> Users { get; set; } = null!;
        public virtual DbSet<Waypoint> Waypoints { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=TrackSync;Username=postgres;Password=popgg006");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Bid>(entity =>
            {
                entity.ToTable("bids");

                entity.Property(e => e.BidId).HasColumnName("bid_id");

                entity.Property(e => e.HikeRoomId).HasColumnName("hike_room_id");

                entity.Property(e => e.UserId).HasColumnName("user_id");

                entity.HasOne(d => d.HikeRoom)
                    .WithMany(p => p.Bids)
                    .HasForeignKey(d => d.HikeRoomId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("bids_hike_room_id_fkey");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.Bids)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("bids_user_id_fkey");
            });

            modelBuilder.Entity<Equipment>(entity =>
            {
                entity.ToTable("equipments");

                entity.HasIndex(e => e.CreatorUserId, "idx_equipments_creator_user_id");

                entity.Property(e => e.EquipmentId).HasColumnName("equipment_id");

                entity.Property(e => e.CreatorUserId).HasColumnName("creator_user_id");

                entity.Property(e => e.Description).HasColumnName("description");

                entity.Property(e => e.Title)
                    .HasMaxLength(256)
                    .HasColumnName("title");

                entity.Property(e => e.Weight).HasColumnName("weight");

                entity.HasOne(d => d.CreatorUser)
                    .WithMany(p => p.Equipment)
                    .HasForeignKey(d => d.CreatorUserId)
                    .HasConstraintName("equipments_creator_user_id_fkey");
            });

            modelBuilder.Entity<HikeRoom>(entity =>
            {
                entity.ToTable("hike_rooms");

                entity.Property(e => e.HikeRoomId).HasColumnName("hike_room_id");

                entity.Property(e => e.AccessType).HasColumnName("access_type");

                entity.Property(e => e.Description).HasColumnName("description");

                entity.Property(e => e.EndDate).HasColumnName("end_date");

                entity.Property(e => e.Password)
                    .HasMaxLength(256)
                    .HasColumnName("password");

                entity.Property(e => e.StartDate).HasColumnName("start_date");

                entity.Property(e => e.StatusId).HasColumnName("status_id");

                entity.Property(e => e.Title)
                    .HasMaxLength(256)
                    .HasColumnName("title");

                entity.HasOne(d => d.Status)
                    .WithMany(p => p.HikeRooms)
                    .HasForeignKey(d => d.StatusId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("hike_rooms_status_id_fkey");
            });

            modelBuilder.Entity<HikeRoomsEquipment>(entity =>
            {
                entity.HasKey(e => e.HikeRoomEquipmentId)
                    .HasName("hike_rooms_equipments_pkey");

                entity.ToTable("hike_rooms_equipments");

                entity.Property(e => e.HikeRoomEquipmentId).HasColumnName("hike_room_equipment_id");

                entity.Property(e => e.EquipmentId).HasColumnName("equipment_id");

                entity.Property(e => e.HikeRoomId).HasColumnName("hike_room_id");

                entity.Property(e => e.ParticipantId).HasColumnName("participant_id");

                entity.Property(e => e.Quantity).HasColumnName("quantity");

                entity.HasOne(d => d.Equipment)
                    .WithMany(p => p.HikeRoomsEquipments)
                    .HasForeignKey(d => d.EquipmentId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("hike_rooms_equipments_equipment_id_fkey");

                entity.HasOne(d => d.HikeRoom)
                    .WithMany(p => p.HikeRoomsEquipments)
                    .HasForeignKey(d => d.HikeRoomId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("hike_rooms_equipments_hike_room_id_fkey");

                entity.HasOne(d => d.Participant)
                    .WithMany(p => p.HikeRoomsEquipments)
                    .HasForeignKey(d => d.ParticipantId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("hike_rooms_equipments_participant_id_fkey");
            });

            modelBuilder.Entity<HikeRoomsRecipe>(entity =>
            {
                entity.HasKey(e => e.HikeRoomRecipeId)
                    .HasName("hike_rooms_recipes_pkey");

                entity.ToTable("hike_rooms_recipes");

                entity.Property(e => e.HikeRoomRecipeId).HasColumnName("hike_room_recipe_id");

                entity.Property(e => e.HikeRoomId).HasColumnName("hike_room_id");

                entity.Property(e => e.MealDate).HasColumnName("meal_date");

                entity.Property(e => e.RecipeId).HasColumnName("recipe_id");

                entity.Property(e => e.TypeMealId).HasColumnName("type_meal_id");

                entity.HasOne(d => d.HikeRoom)
                    .WithMany(p => p.HikeRoomsRecipes)
                    .HasForeignKey(d => d.HikeRoomId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("hike_rooms_recipes_hike_room_id_fkey");

                entity.HasOne(d => d.Recipe)
                    .WithMany(p => p.HikeRoomsRecipes)
                    .HasForeignKey(d => d.RecipeId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("hike_rooms_recipes_recipe_id_fkey");

                entity.HasOne(d => d.TypeMeal)
                    .WithMany(p => p.HikeRoomsRecipes)
                    .HasForeignKey(d => d.TypeMealId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("hike_rooms_recipes_type_meal_id_fkey");
            });

            modelBuilder.Entity<Ingredient>(entity =>
            {
                entity.ToTable("ingredients");

                entity.HasIndex(e => e.CreatorUserId, "idx_ingredients_creator_user_id");

                entity.Property(e => e.IngredientId).HasColumnName("ingredient_id");

                entity.Property(e => e.Calories).HasColumnName("calories");

                entity.Property(e => e.Carbs).HasColumnName("carbs");

                entity.Property(e => e.CreatorUserId).HasColumnName("creator_user_id");

                entity.Property(e => e.Fats).HasColumnName("fats");

                entity.Property(e => e.Price).HasColumnName("price");

                entity.Property(e => e.Proteins).HasColumnName("proteins");

                entity.Property(e => e.RecommendedWeight)
                    .HasMaxLength(40)
                    .HasColumnName("recommended_weight");

                entity.Property(e => e.Title)
                    .HasMaxLength(256)
                    .HasColumnName("title");

                entity.HasOne(d => d.CreatorUser)
                    .WithMany(p => p.Ingredients)
                    .HasForeignKey(d => d.CreatorUserId)
                    .HasConstraintName("ingredients_creator_user_id_fkey");
            });

            modelBuilder.Entity<IngredientsRecipe>(entity =>
            {
                entity.HasKey(e => e.IngredientRecipeId)
                    .HasName("ingredients_recipes_pkey");

                entity.ToTable("ingredients_recipes");

                entity.Property(e => e.IngredientRecipeId).HasColumnName("ingredient_recipe_id");

                entity.Property(e => e.IngredientId).HasColumnName("ingredient_id");

                entity.Property(e => e.RecipeId).HasColumnName("recipe_id");

                entity.Property(e => e.Weight).HasColumnName("weight");

                entity.HasOne(d => d.Ingredient)
                    .WithMany(p => p.IngredientsRecipes)
                    .HasForeignKey(d => d.IngredientId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("ingredients_recipes_ingredient_id_fkey");

                entity.HasOne(d => d.Recipe)
                    .WithMany(p => p.IngredientsRecipes)
                    .HasForeignKey(d => d.RecipeId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("ingredients_recipes_recipe_id_fkey");
            });

            modelBuilder.Entity<Invitation>(entity =>
            {
                entity.ToTable("invitations");

                entity.Property(e => e.InvitationId).HasColumnName("invitation_id");

                entity.Property(e => e.HikeRoomId).HasColumnName("hike_room_id");

                entity.Property(e => e.UserId).HasColumnName("user_id");

                entity.HasOne(d => d.HikeRoom)
                    .WithMany(p => p.Invitations)
                    .HasForeignKey(d => d.HikeRoomId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("invitations_hike_room_id_fkey");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.Invitations)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("invitations_user_id_fkey");
            });

            modelBuilder.Entity<Loginattempt>(entity =>
            {
                entity.HasKey(e => e.AttemptId)
                    .HasName("loginattempts_pkey");

                entity.ToTable("loginattempts");

                entity.HasIndex(e => e.IpAddress, "idx_login_attempts_ip");

                entity.Property(e => e.AttemptId).HasColumnName("attempt_id");

                entity.Property(e => e.AttemptTime)
                    .HasColumnType("timestamp without time zone")
                    .HasColumnName("attempt_time")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.Email)
                    .HasMaxLength(256)
                    .HasColumnName("email");

                entity.Property(e => e.IpAddress)
                    .HasMaxLength(45)
                    .HasColumnName("ip_address");

                entity.Property(e => e.Success).HasColumnName("success");
            });

            modelBuilder.Entity<Participant>(entity =>
            {
                entity.ToTable("participants");

                entity.Property(e => e.ParticipantId).HasColumnName("participant_id");

                entity.Property(e => e.EditEquipment)
                    .IsRequired()
                    .HasColumnName("edit_equipment")
                    .HasDefaultValueSql("true");

                entity.Property(e => e.EditMealPlan)
                    .IsRequired()
                    .HasColumnName("edit_meal_plan")
                    .HasDefaultValueSql("true");

                entity.Property(e => e.EditTrack)
                    .IsRequired()
                    .HasColumnName("edit_track")
                    .HasDefaultValueSql("true");

                entity.Property(e => e.HikeRoomId).HasColumnName("hike_room_id");

                entity.Property(e => e.UserId).HasColumnName("user_id");

                entity.HasOne(d => d.HikeRoom)
                    .WithMany(p => p.Participants)
                    .HasForeignKey(d => d.HikeRoomId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("participants_hike_room_id_fkey");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.Participants)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("participants_user_id_fkey");
            });

            modelBuilder.Entity<Recipe>(entity =>
            {
                entity.ToTable("recipes");

                entity.HasIndex(e => e.CreatorUserId, "idx_recipes_creator_user_id");

                entity.Property(e => e.RecipeId).HasColumnName("recipe_id");

                entity.Property(e => e.CreatorUserId).HasColumnName("creator_user_id");

                entity.Property(e => e.Description).HasColumnName("description");

                entity.Property(e => e.Time).HasColumnName("time");

                entity.Property(e => e.Title)
                    .HasMaxLength(256)
                    .HasColumnName("title");

                entity.HasOne(d => d.CreatorUser)
                    .WithMany(p => p.Recipes)
                    .HasForeignKey(d => d.CreatorUserId)
                    .HasConstraintName("recipes_creator_user_id_fkey");
            });

            modelBuilder.Entity<RecipesParticipant>(entity =>
            {
                entity.HasKey(e => e.RecipeParticipantId)
                    .HasName("recipes_participants_pkey");

                entity.ToTable("recipes_participants");

                entity.Property(e => e.RecipeParticipantId).HasColumnName("recipe_participant_id");

                entity.Property(e => e.ParticipantId).HasColumnName("participant_id");

                entity.Property(e => e.RecipeId).HasColumnName("recipe_id");

                entity.HasOne(d => d.Participant)
                    .WithMany(p => p.RecipesParticipants)
                    .HasForeignKey(d => d.ParticipantId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("recipes_participants_participant_id_fkey");

                entity.HasOne(d => d.Recipe)
                    .WithMany(p => p.RecipesParticipants)
                    .HasForeignKey(d => d.RecipeId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("recipes_participants_recipe_id_fkey");
            });

            modelBuilder.Entity<Segment>(entity =>
            {
                entity.ToTable("segments");

                entity.Property(e => e.SegmentId).HasColumnName("segment_id");

                entity.Property(e => e.Distance).HasColumnName("distance");

                entity.Property(e => e.EndWaypointId).HasColumnName("end_waypoint_id");

                entity.Property(e => e.StartWaypointId).HasColumnName("start_waypoint_id");

                entity.Property(e => e.TrackId).HasColumnName("track_id");

                entity.HasOne(d => d.EndWaypoint)
                    .WithMany(p => p.SegmentEndWaypoints)
                    .HasForeignKey(d => d.EndWaypointId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("segments_end_waypoint_id_fkey");

                entity.HasOne(d => d.StartWaypoint)
                    .WithMany(p => p.SegmentStartWaypoints)
                    .HasForeignKey(d => d.StartWaypointId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("segments_start_waypoint_id_fkey");

                entity.HasOne(d => d.Track)
                    .WithMany(p => p.Segments)
                    .HasForeignKey(d => d.TrackId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("segments_track_id_fkey");
            });

            modelBuilder.Entity<Status>(entity =>
            {
                entity.ToTable("statuses");

                entity.Property(e => e.StatusId).HasColumnName("status_id");

                entity.Property(e => e.Title)
                    .HasMaxLength(20)
                    .HasColumnName("title");
            });

            modelBuilder.Entity<Track>(entity =>
            {
                entity.ToTable("tracks");

                entity.Property(e => e.TrackId).HasColumnName("track_id");

                entity.Property(e => e.HikeRoomId).HasColumnName("hike_room_id");

                entity.Property(e => e.Title)
                    .HasMaxLength(256)
                    .HasColumnName("title");

                entity.HasOne(d => d.HikeRoom)
                    .WithMany(p => p.Tracks)
                    .HasForeignKey(d => d.HikeRoomId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("tracks_hike_room_id_fkey");
            });

            modelBuilder.Entity<TypeMeal>(entity =>
            {
                entity.ToTable("type_meals");

                entity.Property(e => e.TypeMealId).HasColumnName("type_meal_id");

                entity.Property(e => e.Title)
                    .HasMaxLength(20)
                    .HasColumnName("title");
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("users");

                entity.Property(e => e.UserId).HasColumnName("user_id");

                entity.Property(e => e.Contacts)
                    .HasMaxLength(256)
                    .HasColumnName("contacts");

                entity.Property(e => e.Height).HasColumnName("height");

                entity.Property(e => e.Name)
                    .HasMaxLength(256)
                    .HasColumnName("name");

                entity.Property(e => e.Password)
                    .HasMaxLength(256)
                    .HasColumnName("password");

                entity.Property(e => e.Weight).HasColumnName("weight");
            });

            modelBuilder.Entity<Waypoint>(entity =>
            {
                entity.ToTable("waypoints");

                entity.Property(e => e.WaypointId).HasColumnName("waypoint_id");

                entity.Property(e => e.Height).HasColumnName("height");

                entity.Property(e => e.Latitude)
                    .HasPrecision(9, 6)
                    .HasColumnName("latitude");

                entity.Property(e => e.Longitude)
                    .HasPrecision(9, 6)
                    .HasColumnName("longitude");

                entity.Property(e => e.Title)
                    .HasMaxLength(256)
                    .HasColumnName("title");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
