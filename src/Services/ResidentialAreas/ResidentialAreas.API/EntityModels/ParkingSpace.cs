namespace ResidentialAreas.API.EntityModels
{
    [Table("ParkingSpaces")]
    [Index(nameof(Name),nameof(AreaId), IsUnique = true)]
    public class ParkingSpace
    {
        [Key]
        [Required(ErrorMessage = "The Parking Space ID is required.")]
        public Guid Id { get; set; }



        [Required(ErrorMessage = "The Parking Space Name is required.")]
        public string? Name { get; set; }



        public string? Description { get; set; }



        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long ParkingSpaceCode { get; set; }



        [Required(ErrorMessage = "The Area ID is required.")]
        public Guid? AreaId { get; set; }



        [ForeignKey(nameof(AreaId))]
        public Area? Area { get; set; }




        [Required(ErrorMessage = "The Block Number is required.")]
        public string? BlockNo { get; set; }


        [Required(ErrorMessage = "The status is required.")]
        public Status Status { get; set; } = Status.Active;



        public DateTime? CreatedAt { get; set; }



        public DateTime? UpdatedAt {get; set; }


        
        
        public virtual ICollection<ParkingSlot?>? ParkingSlots { get; set; } = new List<ParkingSlot?>();



        public virtual ICollection<Building?>? Buildings { get; set; }= new List<Building?>();


        public virtual ICollection<Image>? Images { get; set; } = new List<Image>();

    }
}
