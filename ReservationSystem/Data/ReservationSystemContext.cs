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
    }
}
