using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ResidentialAreas.API.Migrations
{
    /// <inheritdoc />
    public partial class AddingRelationBetweenImageAndArea : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "AK_Areas_Code",
                table: "Areas",
                column: "Code");

            migrationBuilder.AddForeignKey(
                name: "FK_Images_Areas_Code",
                table: "Images",
                column: "Code",
                principalTable: "Areas",
                principalColumn: "Code",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Images_Areas_Code",
                table: "Images");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Areas_Code",
                table: "Areas");
        }
    }
}
