using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AuthenticationService.API.EntityModels
{
    [Table("UserRoles")]
    [PrimaryKey(nameof(UserId), nameof(RoleId))]
    public class UserRole
    {
        [Required]
        public Guid UserId { get; set; }

        [Required]
        public Guid RoleId { get; set; }

        [Required]
        [Column(TypeName = "datetime2")]
        public DateTime AssignedAt { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual User? User { get; set; }

        [ForeignKey(nameof(RoleId))]
        public virtual Role? Role { get; set; }
    }
}