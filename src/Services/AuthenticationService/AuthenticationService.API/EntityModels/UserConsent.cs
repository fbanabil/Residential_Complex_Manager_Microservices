using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AuthenticationService.API.EntityModels
{
    [Table("UserConsents")]
    [Index(nameof(UserId), nameof(ClientId), IsUnique = true)]
    public class UserConsent
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public Guid ClientId { get; set; }

        [Required]
        public string ScopeList { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "datetime2")]
        public DateTime GrantedAt { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual User? User { get; set; }

        [ForeignKey(nameof(ClientId))]
        public virtual OAuthClient? Client { get; set; }
    }
}