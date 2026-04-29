using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AuthenticationService.API.Enum;
using Microsoft.EntityFrameworkCore;

namespace AuthenticationService.API.EntityModels
{
    [Table("Images")]
    [Index(nameof(UserId))]
    public class Image
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        public string ImagePath { get; set; } = string.Empty;

        [Required]
        public ImageTypes ImageType { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual User? User { get; set; }
    }
}
