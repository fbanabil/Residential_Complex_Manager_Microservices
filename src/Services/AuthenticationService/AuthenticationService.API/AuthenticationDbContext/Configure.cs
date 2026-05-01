using AuthenticationService.API.EntityModels;
using Microsoft.EntityFrameworkCore;

namespace AuthenticationService.API.AuthenticationDbContest
{
    public static class Configure
    {
        public static void ConfigureDb(this ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>()
                .Property(u => u.Status)
                .HasConversion<string>()
                .HasMaxLength(20);

            modelBuilder.Entity<User>()
                .Property(u => u.AuthProvider)
                .HasConversion<string>()
                .HasMaxLength(30);

            modelBuilder.Entity<SecurityTokens>()
                .Property(st => st.Type)
                .HasConversion<string>()
                .HasMaxLength(20);

            modelBuilder.Entity<Image>()
                .Property(i => i.ImageType)
                .HasConversion<string>()
                .HasMaxLength(20);


            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RefreshToken>()
                .HasOne(rt => rt.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RefreshToken>()
                .HasOne(rt => rt.Client)
                .WithMany(c => c.RefreshTokens)
                .HasForeignKey(rt => rt.ClientId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserConsent>()
                .HasOne(uc => uc.User)
                .WithMany(u => u.UserConsents)
                .HasForeignKey(uc => uc.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserConsent>()
                .HasOne(uc => uc.Client)
                .WithMany(c => c.UserConsents)
                .HasForeignKey(uc => uc.ClientId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Image>()
                .HasOne(i => i.User)
                .WithMany(u => u.Images)
                .HasForeignKey(i => i.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<User>()
                .HasOne(u => u.ProfileImage)
                .WithMany()
                .HasForeignKey(u => u.ProfileImageId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<User>()
                .HasOne(u => u.NidImage)
                .WithMany()
                .HasForeignKey(u => u.NidImageId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<SecurityTokens>()
                .HasOne(st => st.User)
                .WithMany()
                .HasForeignKey(st => st.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
