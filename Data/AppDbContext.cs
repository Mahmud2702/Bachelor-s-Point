using Bachelor_s_Point.Models;
using Microsoft.EntityFrameworkCore;

namespace Bachelor_s_Point.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; } = null!;

        public DbSet<Role> Roles { get; set; } = null!;

        public DbSet<Room> Rooms { get; set; } = null!;

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
                    RoleDescription = "Can post available bachelor rooms"
                },
                new Role
                {
                    Id = 3,
                    RoleName = "RoomSeeker",
                    RoleDescription = "Can search and rent bachelor rooms"
                }
            );
        }
    }
}
