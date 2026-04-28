using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AuthenticationService.API.Enum;
using Microsoft.EntityFrameworkCore;

namespace AuthenticationService.API.EntityModels
{
    [Table("Users")]
    [Index(nameof(Username), IsUnique = true)]
    [Index(nameof(Email), IsUnique = true)]
    public class User
    {
        [Key]
        [Required]
        public Guid Id { get; set; }




        [Required]
        [StringLength(30, ErrorMessage = "Username cannot be longer than 30 characters.")]
        public string Username { get; set; } = string.Empty;

        
        
        
        [Required(ErrorMessage = "Email is required.")]
        [StringLength(150, ErrorMessage = "Email cannot be longer than 150 characters.")]
        [EmailAddress(ErrorMessage = "Email must be a valid email address.")]
        public string Email { get; set; } = string.Empty;

        
        
        
        [StringLength(30, ErrorMessage = "Phone number cannot be longer than 30 characters.")]
        public string? Phone { get; set; }

        
        
        
        [StringLength(500, ErrorMessage = "Password hash cannot be longer than 500 characters.")]
        public string? PasswordHash { get; set; }

        
        
        
        [Required]
        [Column(TypeName = "varchar(20)")]
        public Status Status { get; set; } = Status.Active;

        
        
        
        [Required]
        public bool IsUserVerified { get; set; }

        
        
        
        [Required]
        public bool IsEmailVerified { get; set; }

        
        
        
        [Required]
        [Column(TypeName = "varchar(20)")]
        public AuthProvider AuthProvider { get; set; } = AuthProvider.Local;

        
        
        
        public Guid? ProfileImageId { get; set; }

        [ForeignKey(nameof(ProfileImageId))]
        public virtual Image? ProfileImage { get; set; }

        
        
        
        public Guid? NidImageId { get; set; }

        [ForeignKey(nameof(NidImageId))]
        public virtual Image? NidImage { get; set; }

        
        
        
        [Required]
        [Column(TypeName = "datetime2")]
        public DateTime CreatedAt { get; set; }

        
        
        
        [Required]
        [Column(TypeName = "datetime2")]
        public DateTime UpdatedAt { get; set; }

        
        
        
        [Column(TypeName = "datetime2")]
        public DateTime? LastLoginAt { get; set; }

        
        
        
        public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
        public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
        public virtual ICollection<UserConsent> UserConsents { get; set; } = new List<UserConsent>();
        public virtual ICollection<Image> Images { get; set; } = new List<Image>();
    }
}