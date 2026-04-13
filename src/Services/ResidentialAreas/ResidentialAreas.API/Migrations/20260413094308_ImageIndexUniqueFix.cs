using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ResidentialAreas.API.Migrations
{
    /// <inheritdoc />
    public partial class ImageIndexUniqueFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Images_Code",
                table: "Images");

            migrationBuilder.CreateIndex(
                name: "IX_Images_Code",
                table: "Images",
                column: "Code");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Images_Code",
                table: "Images");

            migrationBuilder.CreateIndex(
                name: "IX_Images_Code",
                table: "Images",
                column: "Code",
                unique: true);
        }
    }
}
