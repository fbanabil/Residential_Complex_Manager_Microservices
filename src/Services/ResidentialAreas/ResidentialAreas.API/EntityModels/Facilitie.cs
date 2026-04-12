namespace ResidentialAreas.API.EntityModels
{
    [Table("Facilities")]
    [Index(nameof(AreaId))]
    [Index(nameof(BuildingId))]
    public class Facilitie
    {
        [Key]
        [Required(ErrorMessage = "The Facility ID is required.")]
        public Guid Id { get; set; }

        
        public Guid? AreaId { get; set; }

        
        [ForeignKey(nameof(AreaId))]
        public Area? Area { get; set; }

        
        public Guid? BuildingId { get; set; }

        
        [ForeignKey(nameof(BuildingId))]
        public Building? Building { get; set; }

        
        [Required(ErrorMessage = "The facility name is required.")]
        [StringLength(100, ErrorMessage = "The facility name cannot exceed 100 characters.")]
        public string Name { get; set; } = string.Empty;

        
        [Required(ErrorMessage = "The facility type is required.")]
        [StringLength(30, ErrorMessage = "The facility type cannot exceed 30 characters.")]
        public string FacilityType { get; set; } = string.Empty;

        
        [Required(ErrorMessage = "The capacity is required.")]
        public int Capacity { get; set; }

        
        [Required(ErrorMessage = "The booking required indicator is required.")]
        public bool BookingRequired { get; set; }

        public decimal? HourlyRate { get; set; }

        
        [Column(TypeName = "jsonb")]
        public string? Rules { get; set; }

        
        [Required(ErrorMessage = "The status is required.")]
        [Column(TypeName = "varchar(20)")]
        public Status Status { get; set; } = Status.Active;

        
        [Required(ErrorMessage = "The creation date is required.")]
        public DateTime CreatedAt { get; set; }

        
        [Required(ErrorMessage = "The last updated date is required.")]
        public DateTime UpdatedAt { get; set; }

        
    }
}
