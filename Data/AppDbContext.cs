using Bachelor_s_Point.Models;
using Microsoft.EntityFrameworkCore;

namespace Bachelor_s_Point.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }

        public DbSet<Role> Roles { get; set; }

        public DbSet<Room> Rooms { get; set; }

        public DbSet<RoomSelection> RoomSelections { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Role -> Users (one to many)
            modelBuilder.Entity<Role>()
                .HasMany(r => r.Users)
                .WithOne(u => u.Role)
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            // User -> Rooms (one to many)
            modelBuilder.Entity<User>()
                .HasMany(u => u.Rooms)
                .WithOne(r => r.Owner)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Email should be unique
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // RoomSelection -> Room (many to one)
            modelBuilder.Entity<RoomSelection>()
                .HasOne(s => s.Room)
                .WithMany()
                .HasForeignKey(s => s.RoomId)
                .OnDelete(DeleteBehavior.Cascade);

            // RoomSelection -> User (Seeker) (many to one)
            // NoAction to prevent multiple cascade paths (User -> Rooms -> Selection AND User -> Selection)
            modelBuilder.Entity<RoomSelection>()
                .HasOne(s => s.Seeker)
                .WithMany()
                .HasForeignKey(s => s.SeekerUserId)
                .OnDelete(DeleteBehavior.NoAction);

            // Seed default roles
            modelBuilder.Entity<Role>().HasData(
                new Role
                {
                    Id = 1,
                    RoleName = "Admin",
                    RoleDescription = "Can manage all users and system data"
                },
                new Role
                {
                    Id = 2,
                    RoleName = "RoomOwner",
                    RoleDescription = "Default user role — can post rooms and select rooms"
                },
                new Role
                {
                    Id = 3,
                    RoleName = "RoomSeeker",
                    RoleDescription = "Legacy role, kept for backward compatibility"
                }
            );
        }
    }
}
