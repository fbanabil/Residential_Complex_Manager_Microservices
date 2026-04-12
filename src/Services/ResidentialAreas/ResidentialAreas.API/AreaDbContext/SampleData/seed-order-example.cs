// Example: simple ordered seeding from generated JSON files
// Note: adjust enum parsing for ImageType / SlotType after you define those enums properly.

await db.Areas.AddRangeAsync(areaList);
await db.SaveChangesAsync();

await db.Buildings.AddRangeAsync(buildingList);
await db.SaveChangesAsync();

await db.Units.AddRangeAsync(unitList);
await db.SaveChangesAsync();

await db.Facilities.AddRangeAsync(facilityList);
await db.SaveChangesAsync();

await db.ParkingSlots.AddRangeAsync(parkingSlotList);
await db.SaveChangesAsync();

await db.Images.AddRangeAsync(imageList);
await db.SaveChangesAsync();