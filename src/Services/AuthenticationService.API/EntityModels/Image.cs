using System.ComponentModel.DataAnnotations;

namespace AuthenticationService.API.EntityModels
{
    public class Image
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        public string? ImagePath { get; set; }

        [Required]
        public Guid? UserId { get; set; }

        public virtual User? User { get; set; }
    }
}
