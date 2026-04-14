namespace ResidentialAreas.API.EntityModels
{
    [Table("Buildings")]
    [Index(nameof(Code), IsUnique = true)]
    [Index(nameof(AreaId))]
    public class Building
    {
        [Key]
        [Required(ErrorMessage = "The Building ID is required.")]
        public Guid? Id { get; set; }


        [Required(ErrorMessage = "The parent Area ID is required.")]
        public Guid? AreaId { get; set; }

        [ForeignKey(nameof(AreaId))]
        public Area? Area { get; set; }


        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Code { get; set; }


        [Required(ErrorMessage = "The building name is required.")]
        [StringLength(100, ErrorMessage = "The building name cannot exceed 100 characters.")]
        public string? Name { get; set; } = string.Empty;

        
        [Required(ErrorMessage = "The block number is required.")]
        [StringLength(30, ErrorMessage = "The block number cannot exceed 30 characters.")]
        public string? BlockNo { get; set; } = string.Empty;

        public string? Address { get; set; }


        [Required(ErrorMessage = "The total floor count is required.")]
        public int? TotalFloors { get; set; }


        [Required(ErrorMessage = "The status is required.")]
        [Column(TypeName = "varchar(20)")]
        public Status Status { get; set; } = Status.Active;

        
        [Required(ErrorMessage = "The creation date is required.")]
        public DateTime? CreatedAt { get; set; }

        
        [Required(ErrorMessage = "The last updated date is required.")]
        public DateTime? UpdatedAt { get; set; }
        public virtual ICollection<Unit>? Units { get; set; } = new List<Unit>();
        public virtual ICollection<Facility>? Facilities { get; set; } = new List<Facility>();
        public virtual ICollection<ParkingSlot>? ParkingSlots { get; set; } = new List<ParkingSlot>();   
    }
}
