using Microsoft.EntityFrameworkCore;
using ReservationSystem.Models;

namespace ReservationSystem.Data
{
    public class ReservationSystemContext : DbContext
    {
        public ReservationSystemContext(DbContextOptions<ReservationSystemContext> options)
            : base(options)
        { }

        public DbSet<Table> Tables { get; set; }
        public DbSet<Reservation> Reservations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Reservation>()
                .HasOne(r => r.Table)
                .WithMany(t => t.Reservations)
                .HasForeignKey(r => r.TableId);  

            modelBuilder.Entity<Reservation>()
                .HasIndex(r => new { r.TableId, r.ReservationTime })
                .IsUnique(); 
        }
    }
}

