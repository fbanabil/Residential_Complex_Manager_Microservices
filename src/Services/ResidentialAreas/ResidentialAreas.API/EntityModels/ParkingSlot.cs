using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ResidentialAreas.API.EntityModels
{
    [Table("ParkingSlots")]
    [Index(nameof(SlotCode), IsUnique = true)]
    public class ParkingSlot
    {
        [Key]
        [Required(ErrorMessage = "The Parking Slot ID is required.")]
        public Guid? Id { get; set; }


        
        public Guid? ParkingSpaceId { get; set; }



        
        [ForeignKey(nameof(ParkingSpaceId))]
        public ParkingSpace? ParkingSpace { get; set; }




        public Guid? AssignedUnitId { get; set; }

        
        [ForeignKey(nameof(AssignedUnitId))]
        public Unit? AssignedUnit { get; set; }





        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long SlotCode { get; set; }

        

       
        
        [Required(ErrorMessage = "The slot type is required.")]
        [Column(TypeName = "varchar(20)")]
        public SlotType SlotType { get; set; } = SlotType.Compact;






        [Required(ErrorMessage = "The status is required.")]
        [Column(TypeName = "varchar(20)")]
        public Status Status { get; set; } = Status.Active;

        
        
        
        
        [Required(ErrorMessage = "The creation date is required.")]
        public DateTime? CreatedAt { get; set; }

        
        
        
        [Required(ErrorMessage = "The last updated date is required.")]
        public DateTime? UpdatedAt { get; set; }

    }
}
