namespace ResidentialAreas.API.EntityModels
{
    [Table("Images")]
    [Index(nameof(AreaCode))]
    [Index(nameof(BuildingCode))]
    [Index(nameof(ParkingSpaceCode))]
    [Index(nameof(UnitCode))]
    [Index(nameof(FacilityCode))]
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
        public long? ParkingSpaceCode { get; set; }
        public long? UnitCode { get; set; }
        public long? FacilityCode { get; set; }


        [Required(ErrorMessage = "The image URL is required.")]
        public string? Url { get; set; }



        [ForeignKey(nameof(AreaCode))]
        public Area? Area { get; set; }


        [ForeignKey(nameof(BuildingCode))]
        public Building? Building { get; set; }


        [ForeignKey(nameof(FacilityCode))]
        public Facility? Facility { get; set; }

        [ForeignKey(nameof(ParkingSpaceCode))]
        public ParkingSpace? ParkingSpace { get; set; }


        [ForeignKey(nameof(UnitCode))]
        public Unit? Unit { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (AreaCode == 0 && BuildingCode == 0 && ParkingSpaceCode == 0 && UnitCode == 0 && FacilityCode == 0)
            {
                yield return new ValidationResult("At least one of AreaCode, BuildingCode, ParkingSpaceCode, UnitCode, or FacilityCode must be provided.", new[] { nameof(AreaCode), nameof(BuildingCode), nameof(ParkingSpaceCode), nameof(UnitCode), nameof(FacilityCode) });
            }
            if (AreaCode == null && BuildingCode == null && ParkingSpaceCode == null && UnitCode == null && FacilityCode == null)
            {
                yield return new ValidationResult("At least one of AreaCode, BuildingCode, ParkingSpaceCode, UnitCode, or FacilityCode must be provided.", new[] { nameof(AreaCode), nameof(BuildingCode), nameof(ParkingSpaceCode), nameof(UnitCode), nameof(FacilityCode) });
            }

            int p = (AreaCode != null && AreaCode != 0 ? 1 : 0) + (BuildingCode != null && BuildingCode != 0 ? 1 : 0) + (ParkingSpaceCode != null && ParkingSpaceCode != 0 ? 1 : 0) + (UnitCode != null && UnitCode != 0 ? 1 : 0) + (FacilityCode != null && FacilityCode != 0 ? 1 : 0);
            if (p > 1)
            {
                yield return new ValidationResult("An image can only be associated with one of Area, Building, Parking Space, Unit, or Facility.", new[] { nameof(AreaCode), nameof(BuildingCode), nameof(ParkingSpaceCode), nameof(UnitCode), nameof(FacilityCode) });
            }
        }
    }
}
