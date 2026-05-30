extern alias AuthApi;
using AuthApi::AuthenticationService.API.Apis.User.ChangePassword;
using AuthApi::AuthenticationService.API.AuthenticationDbContest;
using AuthApi::AuthenticationService.API.EntityModels;
using AuthApi::AuthenticationService.API.Enum;
using AuthApi::AuthenticationService.API.Helpers.PasswordHelper.Hasher;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Residential_Complex_Manager_Tests.Common;

namespace Residential_Complex_Manager_Tests.AuthenticationService.Unit
{
    public class ChangePasswordHandlerTests
    {
        private static (ChangePasswordHandler handler, AuthDbContext ctx, SqliteAuthDbContextScope scope,
                Mock<IPasswordHasher> hasher) Build()
        {
            var scope = new SqliteAuthDbContextScope();
            var hasher = new Mock<IPasswordHasher>();
            return (new ChangePasswordHandler(scope.Context, hasher.Object,
                NullLogger<ChangePasswordHandler>.Instance), scope.Context, scope, hasher);
        }

        private static User Seed(AuthDbContext ctx, string email, string? passwordHash)
        {
            var u = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                Username = "u_" + Guid.NewGuid().ToString("N")[..6],
                PasswordHash = passwordHash,
                Status = Status.Active,
                AuthProvider = AuthProvider.Local,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            ctx.Users.Add(u);
            ctx.SaveChanges();
            return u;
        }

        [Fact]
        public async Task Returns_USER_NOT_FOUND_for_unknown_email()
        {
            var (h, _, scope, _) = Build();
            await using var _s = scope;
            var res = await h.Handle(new ChangePasswordCommand("c", "N!ewPwd1", "N!ewPwd1", "x@y.com"), default);
            res.Error!.Title.Should().Be("USER_NOT_FOUND");
        }

        [Fact]
        public async Task Returns_PASSWORD_NOT_SET_when_user_has_no_password_hash()
        {
            var (h, ctx, scope, _) = Build();
            await using var _s = scope;
            Seed(ctx, "u@x.com", passwordHash: null);
            var res = await h.Handle(new ChangePasswordCommand("c", "N!ewPwd1", "N!ewPwd1", "u@x.com"), default);
            res.Error!.Title.Should().Be("PASSWORD_NOT_SET");
        }

        [Fact]
        public async Task Returns_INVALID_CURRENT_PASSWORD_when_verify_fails()
        {
            var (h, ctx, scope, hasher) = Build();
            await using var _s = scope;
            Seed(ctx, "u@x.com", passwordHash: "HASH");
            hasher.Setup(x => x.VerifyPassword("wrong", "HASH")).ReturnsAsync(false);

            var res = await h.Handle(new ChangePasswordCommand("wrong", "N!ew1", "N!ew1", "u@x.com"), default);
            res.Error!.Title.Should().Be("INVALID_CURRENT_PASSWORD");
        }

        [Fact]
        public async Task Updates_password_hash_on_success()
        {
            var (h, ctx, scope, hasher) = Build();
            await using var _s = scope;
            Seed(ctx, "u@x.com", passwordHash: "OLD_HASH");
            hasher.Setup(x => x.VerifyPassword("oldClear", "OLD_HASH")).ReturnsAsync(true);
            hasher.Setup(x => x.HashPassword("N!ewPwd1")).ReturnsAsync("NEW_HASH");

            var res = await h.Handle(new ChangePasswordCommand("oldClear", "N!ewPwd1", "N!ewPwd1", "u@x.com"), default);

            res.Error.Should().BeNull();
            ctx.Users.AsNoTracking().Single(x => x.Email == "u@x.com").PasswordHash.Should().Be("NEW_HASH");
        }

        [Fact]
        public async Task Handle_rejects_when_new_and_confirm_passwords_do_not_match()
        {
            var (h, ctx, scope, hasher) = Build();
            await using var _s = scope;
            Seed(ctx, "u@x.com", passwordHash: "OLD_HASH");
            hasher.Setup(x => x.VerifyPassword("oldClear", "OLD_HASH")).ReturnsAsync(true);
            hasher.Setup(x => x.HashPassword(It.IsAny<string>())).ReturnsAsync("ANY");

            var res = await h.Handle(new ChangePasswordCommand("oldClear", "DESIRED!1A", "TYPO!1A", "u@x.com"), default);

            res.Error.Should().NotBeNull();
            ctx.Users.AsNoTracking().Single(x => x.Email == "u@x.com").PasswordHash.Should().Be("OLD_HASH");
        }
    }
}
