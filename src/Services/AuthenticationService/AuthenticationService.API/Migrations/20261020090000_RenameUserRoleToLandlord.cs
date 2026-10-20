using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuthenticationService.API.Migrations
{
    /// <inheritdoc />
    public partial class RenameUserRoleToLandlord : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE [Roles] SET [Name] = 'Landlord' WHERE [Name] = 'Tenant';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE [Roles] SET [Name] = 'Tenant' WHERE [Name] = 'Landlord';");
        }
    }
}
