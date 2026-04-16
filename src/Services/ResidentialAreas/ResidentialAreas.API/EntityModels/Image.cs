namespace ResidentialAreas.API.EntityModels
{
    [Table("Images")]
    [Index(nameof(AreaCode))]
    [Index(nameof(BuildingCode))]
    [Index(nameof(ParkingSlotCode))]
    [Index(nameof(UnitCode))]
    public class Image : IValidatableObject
    {
        [Key]
        [Required(ErrorMessage = "The image ID is required.")]
        public Guid? Id { get; set; }


        [Required(ErrorMessage ="The image type is required.")]
        [Column(TypeName = "varchar(20)")]
        public ImageType? ImageType { get; set; }



        public long? AreaCode { get; set; } 
        public long? BuildingCode { get; set; }
        public long? ParkingSlotCode { get; set; }
        public long? UnitCode { get; set; }


        [Required(ErrorMessage = "The image URL is required.")]
        public string? Url { get; set; }



        [ForeignKey(nameof(AreaCode))]
        public Area? Area { get; set; }


        [ForeignKey(nameof(BuildingCode))]
        public Building? Building { get; set; }


        [ForeignKey(nameof(ParkingSlotCode))]
        public ParkingSlot? ParkingSlot { get; set; }


        [ForeignKey(nameof(UnitCode))]
        public Unit? Unit { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (AreaCode == 0 && BuildingCode == 0 && ParkingSlotCode == 0 && UnitCode == 0)
            {
                yield return new ValidationResult("At least one of AreaCode, BuildingCode, ParkingSlotCode, or UnitCode must be provided.", new[] { nameof(AreaCode), nameof(BuildingCode), nameof(ParkingSlotCode), nameof(UnitCode) });
            }
            if (AreaCode == null && BuildingCode == null && ParkingSlotCode == null && UnitCode == null)
            {
                yield return new ValidationResult("At least one of AreaCode, BuildingCode, ParkingSlotCode, or UnitCode must be provided.", new[] { nameof(AreaCode), nameof(BuildingCode), nameof(ParkingSlotCode), nameof(UnitCode) });
            }

            int p = (AreaCode != null && AreaCode != 0 ? 1 : 0) + (BuildingCode != null && BuildingCode != 0 ? 1 : 0) + (ParkingSlotCode != null && ParkingSlotCode != 0 ? 1 : 0) + (UnitCode != null && UnitCode != 0 ? 1 : 0);
            if (p > 1)
            {
                yield return new ValidationResult("An image can only be associated with one of Area, Building, Parking Slot, or Unit.", new[] { nameof(AreaCode), nameof(BuildingCode), nameof(ParkingSlotCode), nameof(UnitCode) });
            }
        }
    }
}
