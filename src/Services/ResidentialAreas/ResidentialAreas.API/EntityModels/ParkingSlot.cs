using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ResidentialAreas.API.EntityModels
{
    [Table("ParkingSlots")]
    [Index(nameof(SlotCode), IsUnique = true)]
    [Index(nameof(BuildingId))]
    [Index(nameof(AssignedResidentId))]
    public class ParkingSlot
    {
        [Key]
        [Required(ErrorMessage = "The Parking Slot ID is required.")]
        public Guid? Id { get; set; }

        
        public Guid? BuildingId { get; set; }

        
        [ForeignKey(nameof(BuildingId))]
        public Building? Building { get; set; }

        
        public Guid? UnitId { get; set; }

        
        [ForeignKey(nameof(UnitId))]
        public Unit? Unit { get; set; }

        
        [Required(ErrorMessage = "The slot code is required.")]
        [StringLength(20, ErrorMessage = "The slot code cannot exceed 20 characters.")]
        public string? SlotCode { get; set; } = string.Empty;

        
        [Required(ErrorMessage = "The slot type is required.")]
        [Column(TypeName = "varchar(20)")]
        public SlotType SlotType { get; set; } = SlotType.Compact;



        public Guid? AssignedResidentId { get; set; }

        
        [Required(ErrorMessage = "The status is required.")]
        [Column(TypeName = "varchar(20)")]
        public Status Status { get; set; } = Status.Active;

        
        [Required(ErrorMessage = "The creation date is required.")]
        public DateTime? CreatedAt { get; set; }

        
        [Required(ErrorMessage = "The last updated date is required.")]
        public DateTime? UpdatedAt { get; set; }
    }
}
