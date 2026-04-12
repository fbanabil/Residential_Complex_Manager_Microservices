namespace ResidentialAreas.API.EntityModels
{
    [Table("Units")]
    [Index(nameof(BuildingId), nameof(UnitNo), IsUnique = true)] 
    public class Unit
    {
        [Key]
        [Required(ErrorMessage = "The Unit ID is required.")]
        public Guid Id { get; set; }

        
        [Required(ErrorMessage = "The parent Building ID is required.")]
        public Guid BuildingId { get; set; }

        
        [ForeignKey(nameof(BuildingId))]
        public Building? Building { get; set; }

        
        [Required(ErrorMessage = "The unit number is required.")]
        [StringLength(20, ErrorMessage = "The unit number cannot exceed 20 characters.")]
        public string UnitNo { get; set; } = string.Empty;


        [Required(ErrorMessage = "The unit code is required.")]
        [StringLength(20, ErrorMessage = "The unit code cannot exceed 20 characters.")]
        public string? Code { get; set; }


        [Required(ErrorMessage = "The floor number is required.")]
        public int FloorNo { get; set; }

        
        [Required(ErrorMessage = "The unit type is required.")]
        [Column(TypeName = "varchar(20)")]
        public UnitType UnitType { get; set; } 

        
        public int? Bedrooms { get; set; }
        
        
        public int? Bathrooms { get; set; }

        
        [Required(ErrorMessage = "The area measurement is required.")]
        [Column(TypeName = "decimal(10,2)")]
        public decimal AreaSqft { get; set; }


        [Required(ErrorMessage = "The occupancy status is required.")]
        [Column(TypeName = "varchar(20)")]
        public OccupancyStatus OccupancyStatus { get; set; } = OccupancyStatus.Vacant; 

        
        [Required(ErrorMessage = "The ownership type is required.")]
        [Column(TypeName = "varchar(20)")]
        public OwnershipType OwnershipType { get; set; } = OwnershipType.Owned; 
        
        public Guid? CurrentLeaseId { get; set; }

        
        [Required(ErrorMessage = "The creation date is required.")]
        public DateTime CreatedAt { get; set; }

        
        [Required(ErrorMessage = "The last updated date is required.")]
        public DateTime UpdatedAt { get; set; }


        public virtual ICollection<Facility> Facilities { get; set; } = new List<Facility>();
        public virtual ICollection<ParkingSlot> ParkingSlots { get; set; } = new List<ParkingSlot>();


    }
}
