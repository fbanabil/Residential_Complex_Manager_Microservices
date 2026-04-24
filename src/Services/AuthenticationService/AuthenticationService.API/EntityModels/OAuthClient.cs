    using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AuthenticationService.API.EntityModels
{
    [Table("OAuthClients")]
    [Index(nameof(ClientId), IsUnique = true)]
    public class OAuthClient
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [StringLength(100)]
        public string ClientId { get; set; } = string.Empty;

        [StringLength(500)]
        public string? ClientSecretHash { get; set; }

        [Required]
        [StringLength(100)]
        public string ClientName { get; set; } = string.Empty;

        [Required]
        [StringLength(300)]
        public string GrantTypes { get; set; } = string.Empty;

        [Required]
        public string RedirectUris { get; set; } = string.Empty;

        [Required]
        public string AllowedScopes { get; set; } = string.Empty;

        [Required]
        public int AccessTokenLifetimeSeconds { get; set; }

        [Required]
        public int RefreshTokenLifetimeSeconds { get; set; }

        [Required]
        public bool RequirePkce { get; set; }

        [Required]
        public bool IsConfidentialClient { get; set; }

        [Required]
        public bool IsActive { get; set; }

        public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

        public virtual ICollection<UserConsent> UserConsents { get; set; } = new List<UserConsent>();
    }
}