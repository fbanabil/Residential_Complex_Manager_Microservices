using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ResidentialAreas.API.Migrations
{
    /// <inheritdoc />
    public partial class RenameBuildingOwnerToLandlord : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TenantId",
                table: "Buildings",
                newName: "LandlordId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LandlordId",
                table: "Buildings",
                newName: "TenantId");
        }
    }
}
