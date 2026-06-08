using Bachelor_s_Point.Models;
using Microsoft.EntityFrameworkCore;

namespace Bachelor_s_Point.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User>                Users                { get; set; }
        public DbSet<Role>                Roles                { get; set; }
        public DbSet<Room>                Rooms                { get; set; }
        public DbSet<RoomSelection>       RoomSelections       { get; set; }
        public DbSet<RoomImage>           RoomImages           { get; set; }
        public DbSet<ChatMessage>         ChatMessages         { get; set; }
        public DbSet<PendingRegistration> PendingRegistrations { get; set; }
        public DbSet<PasswordResetToken>  PasswordResetTokens  { get; set; }
        public DbSet<KycVerification>     KycVerifications     { get; set; }
        public DbSet<LoginHistory>        LoginHistories       { get; set; }
        public DbSet<Payment>             Payments             { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Role>()
                .HasMany(r => r.Users)
                .WithOne(u => u.Role)
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<User>()
                .HasMany(u => u.Rooms)
                .WithOne(r => r.Owner)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<RoomSelection>()
                .HasOne(s => s.Room)
                .WithMany()
                .HasForeignKey(s => s.RoomId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RoomSelection>()
                .HasOne(s => s.Seeker)
                .WithMany()
                .HasForeignKey(s => s.SeekerUserId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Room>()
                .HasMany(r => r.Images)
                .WithOne(i => i.Room)
                .HasForeignKey(i => i.RoomId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ChatMessage>()
                .HasOne(m => m.Sender)
                .WithMany()
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ChatMessage>()
                .HasOne(m => m.Receiver)
                .WithMany()
                .HasForeignKey(m => m.ReceiverId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<PendingRegistration>()
                .HasIndex(p => p.Email);

            modelBuilder.Entity<PasswordResetToken>()
                .HasIndex(p => p.Email);

            modelBuilder.Entity<KycVerification>()
                .HasIndex(k => k.UserId)
                .IsUnique();
            modelBuilder.Entity<KycVerification>()
                .HasOne(k => k.User)
                .WithMany()
                .HasForeignKey(k => k.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LoginHistory>()
                .HasOne(l => l.User)
                .WithMany()
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Payment
            modelBuilder.Entity<Payment>()
                .HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Room)
                .WithMany()
                .HasForeignKey(p => p.RoomId)
                .OnDelete(DeleteBehavior.SetNull); // keep payment record if room deleted

            modelBuilder.Entity<Payment>()
                .HasIndex(p => new { p.UserId, p.Type });

            modelBuilder.Entity<Role>().HasData(
                new Role { Id = 1, RoleName = "Admin", RoleDescription = "Can manage all users and system data" },
                new Role { Id = 2, RoleName = "User",  RoleDescription = "Regular user — can post and select rooms" }
            );
        }
    }
}
