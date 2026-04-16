namespace ResidentialAreas.API.AppDbContext
{
    public static class ModelConfigurations
    {
        public static void Configure(this ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Area>()
                .Property(a => a.Code)
                .UseIdentityByDefaultColumn()
                .HasIdentityOptions(startValue: 1000000000);



            modelBuilder.Entity<Building>()
                .Property(b => b.Code)
                .UseIdentityByDefaultColumn()
                .HasIdentityOptions(startValue: 2000000000);


            modelBuilder.Entity<ParkingSlot>()
                .Property(p => p.SlotCode)
                .UseIdentityByDefaultColumn()
                .HasIdentityOptions(startValue: 3000000000);


            modelBuilder.Entity<EntityModels.Unit>()
                .Property(u => u.Code)
                .UseIdentityByDefaultColumn()
                .HasIdentityOptions(startValue: 4000000000);


            modelBuilder.Entity<Area>()
                .HasAlternateKey(a => a.Code);

            modelBuilder.Entity<Image>()
                .HasOne(i => i.Area)
                .WithMany(a => a.Images)
                .HasForeignKey(i => i.AreaCode)
                .HasPrincipalKey(a => a.Code)
                .OnDelete(DeleteBehavior.Cascade);


            modelBuilder.Entity<Image>()
                .HasOne(i => i.Building)
                .WithMany(a => a.Images)
                .HasForeignKey(i => i.BuildingCode)
                .HasPrincipalKey(a => a.Code)
                .OnDelete(DeleteBehavior.Cascade);


            modelBuilder.Entity<Image>()
                .HasOne(i => i.ParkingSlot)
                .WithMany(a => a.Images)
                .HasForeignKey(i => i.ParkingSlotCode)
                .HasPrincipalKey(a => a.SlotCode)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Image>()
                .HasOne(i => i.Unit)
                .WithMany(a => a.Images)
                .HasForeignKey(i => i.UnitCode)
                .HasPrincipalKey(a => a.Code)
                .OnDelete(DeleteBehavior.Cascade);



            modelBuilder.Entity<Area>()
                .Property(a => a.Status)
                .HasConversion<string>();

            modelBuilder.Entity<Image>()
                .Property(i => i.ImageType)
                .HasConversion<string>();

            modelBuilder.Entity<EntityModels.Unit>()
                .Property(u => u.UnitType)
                .HasConversion<string>();

            modelBuilder.Entity<EntityModels.Unit>()
                .Property(u => u.OccupancyStatus)
                .HasConversion<string>();

            modelBuilder.Entity<EntityModels.Unit>()
                .Property(u => u.OwnershipType)
                .HasConversion<string>();

            modelBuilder.Entity<Facility>()
                .Property(f => f.Status)
                .HasConversion<string>();

            modelBuilder.Entity<ParkingSlot>()
                .Property(p => p.Status)
                .HasConversion<string>();

            modelBuilder.Entity<Building>()
                .Property(b => b.Status)
                .HasConversion<string>();

            modelBuilder.Entity<ParkingSlot>()
                .Property(p => p.SlotType)
                .HasConversion<string>();
        }
    }
}
