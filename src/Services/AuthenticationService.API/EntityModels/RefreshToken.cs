using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AuthenticationService.API.EntityModels
{
    [Table("RefreshTokens")]
    [Index(nameof(TokenHash), IsUnique = true)]
    public class RefreshToken
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public Guid ClientId { get; set; }

        [Required]
        [StringLength(500)]
        public string TokenHash { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "datetime2")]
        public DateTime ExpiresAt { get; set; }

        [Column(TypeName = "datetime2")]
        public DateTime? RevokedAt { get; set; }

        [StringLength(300)]
        public string? DeviceInfo { get; set; }

        [Required]
        [Column(TypeName = "datetime2")]
        public DateTime CreatedAt { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual User? User { get; set; }

        [ForeignKey(nameof(ClientId))]
        public virtual OAuthClient? Client { get; set; }
    }
}