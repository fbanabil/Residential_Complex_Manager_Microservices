using System.Text.Json;
using System.Text.Json.Serialization;

namespace ResidentialAreas.API.AppDbContext
{
    public static class InitialData
    {
        public static void Seed(this ModelBuilder modelbuilder)
        {
            string basePath = Path.Combine(AppContext.BaseDirectory, "AppDbContext", "SampleData");

            var options = new JsonSerializerOptions
            {
                Converters = { new JsonStringEnumConverter() }
            };

            var area = File.ReadAllText(Path.Combine(basePath, "Area.sample.json"));
            var areas = JsonSerializer.Deserialize<List<Area>>(area, options);
            if (areas != null)
            {
                modelbuilder.Entity<Area>().HasData(areas);
            }

            var buildings = File.ReadAllText(Path.Combine(basePath, "Building.sample.json"));
            var buildingList = JsonSerializer.Deserialize<List<Building>>(buildings, options);
            if (buildingList != null)
            {
                modelbuilder.Entity<Building>().HasData(buildingList);
            }

            var units = File.ReadAllText(Path.Combine(basePath, "Unit.sample.json"));
            var unitList = JsonSerializer.Deserialize<List<Unit>>(units, options);
            if (unitList != null)
            {
                modelbuilder.Entity<Unit>().HasData(unitList);
            }

            var parkingSpaces = File.ReadAllText(Path.Combine(basePath, "ParkingSlot.sample.json"));
            var parkingSpaceList = JsonSerializer.Deserialize<List<ParkingSlot>>(parkingSpaces, options);
            if (parkingSpaceList != null)
            {
                modelbuilder.Entity<ParkingSlot>().HasData(parkingSpaceList);
            }

            var facilities = File.ReadAllText(Path.Combine(basePath, "Facilitie.sample.json"));
            var facilityList = JsonSerializer.Deserialize<List<Facility>>(facilities, options);
            if (facilityList != null)
            {
                modelbuilder.Entity<Facility>().HasData(facilityList);
            }

            var images = File.ReadAllText(Path.Combine(basePath, "Image.sample.json"));
            var imageList = JsonSerializer.Deserialize<List<Image>>(images, options);
            if (imageList != null)
            {
                modelbuilder.Entity<Image>().HasData(imageList);

            }
        }
    }
}
