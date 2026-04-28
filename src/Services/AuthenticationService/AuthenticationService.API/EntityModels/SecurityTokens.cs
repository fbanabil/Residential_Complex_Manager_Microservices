using AuthenticationService.API.Enum;
using System.ComponentModel.DataAnnotations;

namespace AuthenticationService.API.EntityModels
{
    public class SecurityTokens
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        public string Token { get; set; } = string.Empty;

        [Required]
        public TokenType Type { get; set; }

        [Required]
        public DateTime ExpiresAt { get; set; }

        [Required]
        public bool IsUsed { get; set; } 

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public virtual User? User { get; set; } = null!;
    }
}
