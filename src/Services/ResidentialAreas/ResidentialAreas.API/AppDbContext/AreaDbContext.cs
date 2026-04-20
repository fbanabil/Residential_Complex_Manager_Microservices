using Microsoft.EntityFrameworkCore.Infrastructure;
using ResidentialAreas.API.EntityModels;

namespace ResidentialAreas.API.AppDbContext
{
    public class AreaDbContext : DbContext
    {
        public AreaDbContext(DbContextOptions<AreaDbContext> options) : base(options)
        {
        }
        public virtual DbSet<Area> Areas { get; set; }
        public virtual DbSet<Building> Buildings { get; set; }
        public virtual DbSet<EntityModels.Unit> Units { get; set; } 
        public virtual DbSet<Facility> Facilities { get; set; }
        public virtual DbSet<ParkingSpace> ParkingSpaces { get; set; }
        public virtual DbSet<ParkingSlot> ParkingSlots { get; set; }
        public virtual DbSet<Image> Images { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Configure();

            modelBuilder.Seed();
        }
    }
}
