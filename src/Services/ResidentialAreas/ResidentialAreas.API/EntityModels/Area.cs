namespace ResidentialAreas.API.EntityModels
{
    [Table("Areas")]
    [Index(nameof(Code), IsUnique = true)]
    [Index(nameof(Name))]
    [Index(nameof(City), nameof(State))]
    public class Area
    {
        [Key]
        [Required(ErrorMessage = "The Area ID is required.")]
        public Guid Id { get; set; }




        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Code { get; set; }



        
        [Required(ErrorMessage = "The area name is required.")]
        [StringLength(150, ErrorMessage = "The area name cannot exceed 150 characters.")]
        public string Name { get; set; } = string.Empty;




        [Required(ErrorMessage = "The city is required.")]
        public string City { get; set; } = string.Empty;



        
        [Required(ErrorMessage = "The state is required.")]
        public string State { get; set; } = string.Empty;
        
        
        

        [Required(ErrorMessage = "The country is required.")]
        public string Country { get; set; } = string.Empty;

        

        [Required(ErrorMessage = "The postal code is required.")]
        [StringLength(20, ErrorMessage = "The postal code cannot exceed 20 characters.")]
        public string PostalCode { get; set; } = string.Empty;




        [Required(ErrorMessage = "The address is required.")]
        public string Address { get; set; } = string.Empty;
        
        
        
        
        [Required(ErrorMessage = "The geographical boundary is required.")]
        [Column(TypeName = "jsonb")]
        public string GeoBoundary { get; set; } = string.Empty;

        
        
        
        [Required(ErrorMessage = "The status is required.")]
        [Column(TypeName = "varchar(20)")]
        public Status Status { get; set; } = Status.Active;

        
        
        
        
        [Required(ErrorMessage = "The creation date is required.")]
        public DateTime CreatedAt { get; set; }

        
        
        
        
        [Required(ErrorMessage = "The last updated date is required.")]
        public DateTime UpdatedAt { get; set; }    

       


        public virtual ICollection<ParkingSpace>? ParkingSpaces { get; set; } = new List<ParkingSpace>();

        public virtual ICollection<Image>? Images { get; set; } = new List<Image>();
        
        public virtual ICollection<Building>? Buildings { get; set; } = new List<Building>();
        
        public virtual ICollection<Facility>? Facilities { get; set; } = new List<Facility>();
    }
}
