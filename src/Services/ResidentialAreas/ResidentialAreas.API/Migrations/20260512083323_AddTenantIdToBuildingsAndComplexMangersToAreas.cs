using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ResidentialAreas.API.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantIdToBuildingsAndComplexMangersToAreas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Buildings",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ComplexManagerId",
                table: "Areas",
                type: "uuid",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Areas",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0001-0001-0001-000000000001"),
                column: "ComplexManagerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Areas",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0002-0002-0002-000000000002"),
                column: "ComplexManagerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Areas",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0003-0003-0003-000000000003"),
                column: "ComplexManagerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Areas",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0004-0004-0004-000000000004"),
                column: "ComplexManagerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Areas",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0005-0005-0005-000000000005"),
                column: "ComplexManagerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Areas",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0006-0006-0006-000000000006"),
                column: "ComplexManagerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Areas",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0007-0007-0007-000000000007"),
                column: "ComplexManagerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Areas",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0008-0008-0008-000000000008"),
                column: "ComplexManagerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Areas",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0009-0009-0009-000000000009"),
                column: "ComplexManagerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Areas",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0010-0010-0010-000000000010"),
                column: "ComplexManagerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Areas",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0011-0011-0011-000000000011"),
                column: "ComplexManagerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Areas",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0012-0012-0012-000000000012"),
                column: "ComplexManagerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Areas",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0013-0013-0013-000000000013"),
                column: "ComplexManagerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Areas",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0014-0014-0014-000000000014"),
                column: "ComplexManagerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Areas",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0015-0015-0015-000000000015"),
                column: "ComplexManagerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Areas",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0016-0016-0016-000000000016"),
                column: "ComplexManagerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Areas",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0017-0017-0017-000000000017"),
                column: "ComplexManagerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Areas",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0018-0018-0018-000000000018"),
                column: "ComplexManagerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Areas",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0019-0019-0019-000000000019"),
                column: "ComplexManagerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Areas",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0020-0020-0020-000000000020"),
                column: "ComplexManagerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Buildings",
                keyColumn: "Id",
                keyValue: new Guid("22222222-0001-0001-0001-000000000001"),
                column: "TenantId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Buildings",
                keyColumn: "Id",
                keyValue: new Guid("22222222-0002-0002-0002-000000000002"),
                column: "TenantId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Buildings",
                keyColumn: "Id",
                keyValue: new Guid("22222222-0003-0003-0003-000000000003"),
                column: "TenantId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Buildings",
                keyColumn: "Id",
                keyValue: new Guid("22222222-0004-0004-0004-000000000004"),
                column: "TenantId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Buildings",
                keyColumn: "Id",
                keyValue: new Guid("22222222-0005-0005-0005-000000000005"),
                column: "TenantId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Buildings",
                keyColumn: "Id",
                keyValue: new Guid("22222222-0006-0006-0006-000000000006"),
                column: "TenantId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Buildings",
                keyColumn: "Id",
                keyValue: new Guid("22222222-0007-0007-0007-000000000007"),
                column: "TenantId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Buildings",
                keyColumn: "Id",
                keyValue: new Guid("22222222-0008-0008-0008-000000000008"),
                column: "TenantId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Buildings",
                keyColumn: "Id",
                keyValue: new Guid("22222222-0009-0009-0009-000000000009"),
                column: "TenantId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Buildings",
                keyColumn: "Id",
                keyValue: new Guid("22222222-0010-0010-0010-000000000010"),
                column: "TenantId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Buildings",
                keyColumn: "Id",
                keyValue: new Guid("22222222-0011-0011-0011-000000000011"),
                column: "TenantId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Buildings",
                keyColumn: "Id",
                keyValue: new Guid("22222222-0012-0012-0012-000000000012"),
                column: "TenantId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Buildings",
                keyColumn: "Id",
                keyValue: new Guid("22222222-0013-0013-0013-000000000013"),
                column: "TenantId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Buildings",
                keyColumn: "Id",
                keyValue: new Guid("22222222-0014-0014-0014-000000000014"),
                column: "TenantId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Buildings",
                keyColumn: "Id",
                keyValue: new Guid("22222222-0015-0015-0015-000000000015"),
                column: "TenantId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Buildings",
                keyColumn: "Id",
                keyValue: new Guid("22222222-0016-0016-0016-000000000016"),
                column: "TenantId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Buildings",
                keyColumn: "Id",
                keyValue: new Guid("22222222-0017-0017-0017-000000000017"),
                column: "TenantId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Buildings",
                keyColumn: "Id",
                keyValue: new Guid("22222222-0018-0018-0018-000000000018"),
                column: "TenantId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Buildings",
                keyColumn: "Id",
                keyValue: new Guid("22222222-0019-0019-0019-000000000019"),
                column: "TenantId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Buildings",
                keyColumn: "Id",
                keyValue: new Guid("22222222-0020-0020-0020-000000000020"),
                column: "TenantId",
                value: null);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Buildings");

            migrationBuilder.DropColumn(
                name: "ComplexManagerId",
                table: "Areas");
        }
    }
}
