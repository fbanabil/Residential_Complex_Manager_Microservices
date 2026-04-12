namespace ResidentialAreas.API.AreaDbContext
{
    public class AreaDbContext : DbContext
    {
        public AreaDbContext(DbContextOptions<AreaDbContext> options) : base(options)
        {
        }
        public virtual DbSet<Area> Areas { get; set; }
        public virtual DbSet<Building> Buildings { get; set; }
        public virtual DbSet<Unit> Units { get; set; } 
        public virtual DbSet<Facilitie> Facilities { get; set; }
        public virtual DbSet<ParkingSlot> ParkingSlots { get; set; }
        public virtual DbSet<Image> Images { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Area>()
                .Property(a=>a.Status)
                .HasConversion<string>();
            
            modelBuilder.Entity<Image>()
                .Property(i=>i.ImageType)
                .HasConversion<string>();

            modelBuilder.Entity<Unit>()
                .Property(u=>u.UnitType)
                .HasConversion<string>();

            modelBuilder.Entity<Unit>()
                .Property(u=>u.OccupancyStatus)
                .HasConversion<string>();

            modelBuilder.Entity<Unit>()
                .Property(u=>u.OwnershipType)
                .HasConversion<string>();
            
            modelBuilder.Entity<Facilitie>()
                .Property(f=>f.Status)
                .HasConversion<string>();
            
            modelBuilder.Entity<ParkingSlot>()
                .Property(p=>p.Status)
                .HasConversion<string>();

            modelBuilder.Entity<Building>()
                .Property(b=>b.Status)
                .HasConversion<string>();

            modelBuilder.Entity<ParkingSlot>()
                .Property(p=>p.SlotType)
                .HasConversion<string>();




            modelBuilder.Seed();

        }


    }
}
