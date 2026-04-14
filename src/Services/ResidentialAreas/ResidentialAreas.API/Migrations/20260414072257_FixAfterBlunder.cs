using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ResidentialAreas.API.Migrations
{
    /// <inheritdoc />
    public partial class FixAfterBlunder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Areas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:IdentitySequenceOptions", "'1000000000', '1', '', '', 'False', '1'")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    City = table.Column<string>(type: "text", nullable: false),
                    State = table.Column<string>(type: "text", nullable: false),
                    Country = table.Column<string>(type: "text", nullable: false),
                    PostalCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Address = table.Column<string>(type: "text", nullable: false),
                    GeoBoundary = table.Column<string>(type: "jsonb", nullable: false),
                    Status = table.Column<string>(type: "varchar(20)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Areas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Images",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ImageType = table.Column<string>(type: "varchar(20)", nullable: false),
                    Code = table.Column<long>(type: "bigint", nullable: false),
                    Url = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Images", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Buildings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AreaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:IdentitySequenceOptions", "'2000000000', '1', '', '', 'False', '1'")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    BlockNo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Address = table.Column<string>(type: "text", nullable: true),
                    TotalFloors = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "varchar(20)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Buildings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Buildings_Areas_AreaId",
                        column: x => x.AreaId,
                        principalTable: "Areas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Units",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BuildingId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitNo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Code = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:IdentitySequenceOptions", "'4000000000', '1', '', '', 'False', '1'")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FloorNo = table.Column<int>(type: "integer", nullable: false),
                    UnitType = table.Column<string>(type: "varchar(20)", nullable: false),
                    Bedrooms = table.Column<int>(type: "integer", nullable: true),
                    Bathrooms = table.Column<int>(type: "integer", nullable: true),
                    AreaSqft = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    OccupancyStatus = table.Column<string>(type: "varchar(20)", nullable: false),
                    OwnershipType = table.Column<string>(type: "varchar(20)", nullable: false),
                    CurrentLeaseId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Units", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Units_Buildings_BuildingId",
                        column: x => x.BuildingId,
                        principalTable: "Buildings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Facilities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AreaId = table.Column<Guid>(type: "uuid", nullable: true),
                    BuildingId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FacilityType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Capacity = table.Column<int>(type: "integer", nullable: false),
                    BookingRequired = table.Column<bool>(type: "boolean", nullable: false),
                    HourlyRate = table.Column<decimal>(type: "numeric", nullable: true),
                    Rules = table.Column<string>(type: "jsonb", nullable: true),
                    Status = table.Column<string>(type: "varchar(20)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Facilities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Facilities_Areas_AreaId",
                        column: x => x.AreaId,
                        principalTable: "Areas",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Facilities_Buildings_BuildingId",
                        column: x => x.BuildingId,
                        principalTable: "Buildings",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Facilities_Units_UnitId",
                        column: x => x.UnitId,
                        principalTable: "Units",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ParkingSlots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BuildingId = table.Column<Guid>(type: "uuid", nullable: true),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: true),
                    SlotCode = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:IdentitySequenceOptions", "'3000000000', '1', '', '', 'False', '1'")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SlotType = table.Column<string>(type: "varchar(20)", nullable: false),
                    AssignedResidentId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "varchar(20)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParkingSlots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ParkingSlots_Buildings_BuildingId",
                        column: x => x.BuildingId,
                        principalTable: "Buildings",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ParkingSlots_Units_UnitId",
                        column: x => x.UnitId,
                        principalTable: "Units",
                        principalColumn: "Id");
                });

            migrationBuilder.InsertData(
                table: "Areas",
                columns: new[] { "Id", "Address", "City", "Country", "CreatedAt", "GeoBoundary", "Name", "PostalCode", "State", "Status", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("11111111-0001-0001-0001-000000000001"), "Road 1, Sector 2, Dhaka", "Dhaka", "Bangladesh", new DateTime(2026, 4, 12, 9, 1, 0, 0, DateTimeKind.Utc), "{\"type\": \"Polygon\", \"coordinates\": [[[90.355, 23.705], [90.357, 23.705], [90.357, 23.707], [90.355, 23.707], [90.355, 23.705]]]}", "Dhaka Residential Area 1", "1201", "Dhaka", "Active", new DateTime(2026, 4, 12, 9, 1, 0, 0, DateTimeKind.Utc) },
                    { new Guid("11111111-0002-0002-0002-000000000002"), "Road 2, Sector 3, Chattogram", "Chattogram", "Bangladesh", new DateTime(2026, 4, 12, 9, 2, 0, 0, DateTimeKind.Utc), "{\"type\": \"Polygon\", \"coordinates\": [[[90.36, 23.71], [90.362, 23.71], [90.362, 23.712], [90.36, 23.712], [90.36, 23.71]]]}", "Chattogram Residential Area 2", "1202", "Chattogram", "Active", new DateTime(2026, 4, 12, 9, 2, 0, 0, DateTimeKind.Utc) },
                    { new Guid("11111111-0003-0003-0003-000000000003"), "Road 3, Sector 4, Khulna", "Khulna", "Bangladesh", new DateTime(2026, 4, 12, 9, 3, 0, 0, DateTimeKind.Utc), "{\"type\": \"Polygon\", \"coordinates\": [[[90.365, 23.715], [90.367, 23.715], [90.367, 23.717], [90.365, 23.717], [90.365, 23.715]]]}", "Khulna Residential Area 3", "1203", "Khulna", "Active", new DateTime(2026, 4, 12, 9, 3, 0, 0, DateTimeKind.Utc) },
                    { new Guid("11111111-0004-0004-0004-000000000004"), "Road 4, Sector 5, Rajshahi", "Rajshahi", "Bangladesh", new DateTime(2026, 4, 12, 9, 4, 0, 0, DateTimeKind.Utc), "{\"type\": \"Polygon\", \"coordinates\": [[[90.37, 23.72], [90.372, 23.72], [90.372, 23.722], [90.37, 23.722], [90.37, 23.72]]]}", "Rajshahi Residential Area 4", "1204", "Rajshahi", "Active", new DateTime(2026, 4, 12, 9, 4, 0, 0, DateTimeKind.Utc) },
                    { new Guid("11111111-0005-0005-0005-000000000005"), "Road 5, Sector 6, Sylhet", "Sylhet", "Bangladesh", new DateTime(2026, 4, 12, 9, 5, 0, 0, DateTimeKind.Utc), "{\"type\": \"Polygon\", \"coordinates\": [[[90.375, 23.725], [90.377, 23.725], [90.377, 23.727], [90.375, 23.727], [90.375, 23.725]]]}", "Sylhet Residential Area 5", "1205", "Sylhet", "Active", new DateTime(2026, 4, 12, 9, 5, 0, 0, DateTimeKind.Utc) },
                    { new Guid("11111111-0006-0006-0006-000000000006"), "Road 6, Sector 7, Dhaka", "Dhaka", "Bangladesh", new DateTime(2026, 4, 12, 9, 6, 0, 0, DateTimeKind.Utc), "{\"type\": \"Polygon\", \"coordinates\": [[[90.38, 23.73], [90.382, 23.73], [90.382, 23.732], [90.38, 23.732], [90.38, 23.73]]]}", "Dhaka Residential Area 6", "1206", "Dhaka", "Active", new DateTime(2026, 4, 12, 9, 6, 0, 0, DateTimeKind.Utc) },
                    { new Guid("11111111-0007-0007-0007-000000000007"), "Road 7, Sector 8, Chattogram", "Chattogram", "Bangladesh", new DateTime(2026, 4, 12, 9, 7, 0, 0, DateTimeKind.Utc), "{\"type\": \"Polygon\", \"coordinates\": [[[90.385, 23.735], [90.387, 23.735], [90.387, 23.737], [90.385, 23.737], [90.385, 23.735]]]}", "Chattogram Residential Area 7", "1207", "Chattogram", "Active", new DateTime(2026, 4, 12, 9, 7, 0, 0, DateTimeKind.Utc) },
                    { new Guid("11111111-0008-0008-0008-000000000008"), "Road 8, Sector 1, Khulna", "Khulna", "Bangladesh", new DateTime(2026, 4, 12, 9, 8, 0, 0, DateTimeKind.Utc), "{\"type\": \"Polygon\", \"coordinates\": [[[90.39, 23.74], [90.392, 23.74], [90.392, 23.742], [90.39, 23.742], [90.39, 23.74]]]}", "Khulna Residential Area 8", "1208", "Khulna", "Active", new DateTime(2026, 4, 12, 9, 8, 0, 0, DateTimeKind.Utc) },
                    { new Guid("11111111-0009-0009-0009-000000000009"), "Road 9, Sector 2, Rajshahi", "Rajshahi", "Bangladesh", new DateTime(2026, 4, 12, 9, 9, 0, 0, DateTimeKind.Utc), "{\"type\": \"Polygon\", \"coordinates\": [[[90.395, 23.745], [90.397, 23.745], [90.397, 23.747], [90.395, 23.747], [90.395, 23.745]]]}", "Rajshahi Residential Area 9", "1209", "Rajshahi", "Active", new DateTime(2026, 4, 12, 9, 9, 0, 0, DateTimeKind.Utc) },
                    { new Guid("11111111-0010-0010-0010-000000000010"), "Road 10, Sector 3, Sylhet", "Sylhet", "Bangladesh", new DateTime(2026, 4, 12, 9, 10, 0, 0, DateTimeKind.Utc), "{\"type\": \"Polygon\", \"coordinates\": [[[90.4, 23.75], [90.402, 23.75], [90.402, 23.752], [90.4, 23.752], [90.4, 23.75]]]}", "Sylhet Residential Area 10", "1210", "Sylhet", "Active", new DateTime(2026, 4, 12, 9, 10, 0, 0, DateTimeKind.Utc) },
                    { new Guid("11111111-0011-0011-0011-000000000011"), "Road 11, Sector 4, Dhaka", "Dhaka", "Bangladesh", new DateTime(2026, 4, 12, 9, 11, 0, 0, DateTimeKind.Utc), "{\"type\": \"Polygon\", \"coordinates\": [[[90.405, 23.755], [90.407, 23.755], [90.407, 23.757], [90.405, 23.757], [90.405, 23.755]]]}", "Dhaka Residential Area 11", "1211", "Dhaka", "Active", new DateTime(2026, 4, 12, 9, 11, 0, 0, DateTimeKind.Utc) },
                    { new Guid("11111111-0012-0012-0012-000000000012"), "Road 12, Sector 5, Chattogram", "Chattogram", "Bangladesh", new DateTime(2026, 4, 12, 9, 12, 0, 0, DateTimeKind.Utc), "{\"type\": \"Polygon\", \"coordinates\": [[[90.41, 23.76], [90.412, 23.76], [90.412, 23.762], [90.41, 23.762], [90.41, 23.76]]]}", "Chattogram Residential Area 12", "1212", "Chattogram", "Active", new DateTime(2026, 4, 12, 9, 12, 0, 0, DateTimeKind.Utc) },
                    { new Guid("11111111-0013-0013-0013-000000000013"), "Road 13, Sector 6, Khulna", "Khulna", "Bangladesh", new DateTime(2026, 4, 12, 9, 13, 0, 0, DateTimeKind.Utc), "{\"type\": \"Polygon\", \"coordinates\": [[[90.415, 23.765], [90.417, 23.765], [90.417, 23.767], [90.415, 23.767], [90.415, 23.765]]]}", "Khulna Residential Area 13", "1213", "Khulna", "Active", new DateTime(2026, 4, 12, 9, 13, 0, 0, DateTimeKind.Utc) },
                    { new Guid("11111111-0014-0014-0014-000000000014"), "Road 14, Sector 7, Rajshahi", "Rajshahi", "Bangladesh", new DateTime(2026, 4, 12, 9, 14, 0, 0, DateTimeKind.Utc), "{\"type\": \"Polygon\", \"coordinates\": [[[90.42, 23.77], [90.422, 23.77], [90.422, 23.772], [90.42, 23.772], [90.42, 23.77]]]}", "Rajshahi Residential Area 14", "1214", "Rajshahi", "Active", new DateTime(2026, 4, 12, 9, 14, 0, 0, DateTimeKind.Utc) },
                    { new Guid("11111111-0015-0015-0015-000000000015"), "Road 15, Sector 8, Sylhet", "Sylhet", "Bangladesh", new DateTime(2026, 4, 12, 9, 15, 0, 0, DateTimeKind.Utc), "{\"type\": \"Polygon\", \"coordinates\": [[[90.425, 23.775], [90.427, 23.775], [90.427, 23.777], [90.425, 23.777], [90.425, 23.775]]]}", "Sylhet Residential Area 15", "1215", "Sylhet", "Active", new DateTime(2026, 4, 12, 9, 15, 0, 0, DateTimeKind.Utc) },
                    { new Guid("11111111-0016-0016-0016-000000000016"), "Road 16, Sector 1, Dhaka", "Dhaka", "Bangladesh", new DateTime(2026, 4, 12, 9, 16, 0, 0, DateTimeKind.Utc), "{\"type\": \"Polygon\", \"coordinates\": [[[90.43, 23.78], [90.432, 23.78], [90.432, 23.782], [90.43, 23.782], [90.43, 23.78]]]}", "Dhaka Residential Area 16", "1216", "Dhaka", "Active", new DateTime(2026, 4, 12, 9, 16, 0, 0, DateTimeKind.Utc) },
                    { new Guid("11111111-0017-0017-0017-000000000017"), "Road 17, Sector 2, Chattogram", "Chattogram", "Bangladesh", new DateTime(2026, 4, 12, 9, 17, 0, 0, DateTimeKind.Utc), "{\"type\": \"Polygon\", \"coordinates\": [[[90.435, 23.785], [90.437, 23.785], [90.437, 23.787], [90.435, 23.787], [90.435, 23.785]]]}", "Chattogram Residential Area 17", "1217", "Chattogram", "Active", new DateTime(2026, 4, 12, 9, 17, 0, 0, DateTimeKind.Utc) },
                    { new Guid("11111111-0018-0018-0018-000000000018"), "Road 18, Sector 3, Khulna", "Khulna", "Bangladesh", new DateTime(2026, 4, 12, 9, 18, 0, 0, DateTimeKind.Utc), "{\"type\": \"Polygon\", \"coordinates\": [[[90.44, 23.79], [90.442, 23.79], [90.442, 23.792], [90.44, 23.792], [90.44, 23.79]]]}", "Khulna Residential Area 18", "1218", "Khulna", "Active", new DateTime(2026, 4, 12, 9, 18, 0, 0, DateTimeKind.Utc) },
                    { new Guid("11111111-0019-0019-0019-000000000019"), "Road 19, Sector 4, Rajshahi", "Rajshahi", "Bangladesh", new DateTime(2026, 4, 12, 9, 19, 0, 0, DateTimeKind.Utc), "{\"type\": \"Polygon\", \"coordinates\": [[[90.445, 23.795], [90.447, 23.795], [90.447, 23.797], [90.445, 23.797], [90.445, 23.795]]]}", "Rajshahi Residential Area 19", "1219", "Rajshahi", "Active", new DateTime(2026, 4, 12, 9, 19, 0, 0, DateTimeKind.Utc) },
                    { new Guid("11111111-0020-0020-0020-000000000020"), "Road 20, Sector 5, Sylhet", "Sylhet", "Bangladesh", new DateTime(2026, 4, 12, 9, 20, 0, 0, DateTimeKind.Utc), "{\"type\": \"Polygon\", \"coordinates\": [[[90.45, 23.8], [90.452, 23.8], [90.452, 23.802], [90.45, 23.802], [90.45, 23.8]]]}", "Sylhet Residential Area 20", "1220", "Sylhet", "Active", new DateTime(2026, 4, 12, 9, 20, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "Images",
                columns: new[] { "Id", "Code", "ImageType", "Url" },
                values: new object[,]
                {
                    { new Guid("55555555-0001-0001-0001-000000000001"), 1000000000L, "Area", "https://example.com/images/area/area-dha-001.jpg" },
                    { new Guid("55555555-0002-0002-0002-000000000002"), 2000000001L, "Building", "https://example.com/images/building/bld-cha-002.jpg" },
                    { new Guid("55555555-0003-0003-0003-000000000003"), 4000000003L, "Unit", "https://example.com/images/unit/unit-khu-003.jpg" },
                    { new Guid("55555555-0004-0004-0004-000000000004"), 1000000000L, "Area", "https://example.com/images/area/area-raj-004.jpg" },
                    { new Guid("55555555-0005-0005-0005-000000000005"), 2000000003L, "Building", "https://example.com/images/building/bld-syl-005.jpg" },
                    { new Guid("55555555-0006-0006-0006-000000000006"), 4000000002L, "Unit", "https://example.com/images/unit/unit-dha-006.jpg" },
                    { new Guid("55555555-0007-0007-0007-000000000007"), 1000000003L, "Area", "https://example.com/images/area/area-cha-007.jpg" },
                    { new Guid("55555555-0008-0008-0008-000000000008"), 2000000008L, "Building", "https://example.com/images/building/bld-khu-008.jpg" },
                    { new Guid("55555555-0009-0009-0009-000000000009"), 4000000004L, "Unit", "https://example.com/images/unit/unit-raj-009.jpg" },
                    { new Guid("55555555-0010-0010-0010-000000000010"), 1000000004L, "Area", "https://example.com/images/area/area-syl-010.jpg" },
                    { new Guid("55555555-0011-0011-0011-000000000011"), 2000000011L, "Building", "https://example.com/images/building/bld-dha-011.jpg" },
                    { new Guid("55555555-0012-0012-0012-000000000012"), 4000000005L, "Unit", "https://example.com/images/unit/unit-cha-012.jpg" },
                    { new Guid("55555555-0013-0013-0013-000000000013"), 1000000001L, "Area", "https://example.com/images/area/area-khu-013.jpg" },
                    { new Guid("55555555-0014-0014-0014-000000000014"), 2000000002L, "Building", "https://example.com/images/building/bld-raj-014.jpg" },
                    { new Guid("55555555-0015-0015-0015-000000000015"), 4000000003L, "Unit", "https://example.com/images/unit/unit-syl-015.jpg" },
                    { new Guid("55555555-0016-0016-0016-000000000016"), 1000000004L, "Area", "https://example.com/images/area/area-dha-016.jpg" },
                    { new Guid("55555555-0017-0017-0017-000000000017"), 2000000005L, "Building", "https://example.com/images/building/bld-cha-017.jpg" },
                    { new Guid("55555555-0018-0018-0018-000000000018"), 4000000007L, "Unit", "https://example.com/images/unit/unit-khu-018.jpg" },
                    { new Guid("55555555-0019-0019-0019-000000000019"), 1000000007L, "Area", "https://example.com/images/area/area-raj-019.jpg" },
                    { new Guid("55555555-0020-0020-0020-000000000020"), 2000000011L, "Building", "https://example.com/images/building/bld-syl-020.jpg" }
                });

            migrationBuilder.InsertData(
                table: "Buildings",
                columns: new[] { "Id", "Address", "AreaId", "BlockNo", "CreatedAt", "Name", "Status", "TotalFloors", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("22222222-0001-0001-0001-000000000001"), "Building 1, Dhaka Residential Area 1", new Guid("11111111-0001-0001-0001-000000000001"), "A", new DateTime(2026, 4, 12, 9, 51, 0, 0, DateTimeKind.Utc), "Dhaka Tower 1", "Active", 7, new DateTime(2026, 4, 12, 9, 51, 0, 0, DateTimeKind.Utc) },
                    { new Guid("22222222-0002-0002-0002-000000000002"), "Building 2, Chattogram Residential Area 2", new Guid("11111111-0002-0002-0002-000000000002"), "B", new DateTime(2026, 4, 12, 9, 52, 0, 0, DateTimeKind.Utc), "Chattogram Tower 2", "Active", 8, new DateTime(2026, 4, 12, 9, 52, 0, 0, DateTimeKind.Utc) },
                    { new Guid("22222222-0003-0003-0003-000000000003"), "Building 3, Khulna Residential Area 3", new Guid("11111111-0003-0003-0003-000000000003"), "C", new DateTime(2026, 4, 12, 9, 53, 0, 0, DateTimeKind.Utc), "Khulna Tower 3", "Active", 9, new DateTime(2026, 4, 12, 9, 53, 0, 0, DateTimeKind.Utc) },
                    { new Guid("22222222-0004-0004-0004-000000000004"), "Building 4, Rajshahi Residential Area 4", new Guid("11111111-0004-0004-0004-000000000004"), "D", new DateTime(2026, 4, 12, 9, 54, 0, 0, DateTimeKind.Utc), "Rajshahi Tower 4", "Active", 10, new DateTime(2026, 4, 12, 9, 54, 0, 0, DateTimeKind.Utc) },
                    { new Guid("22222222-0005-0005-0005-000000000005"), "Building 5, Sylhet Residential Area 5", new Guid("11111111-0005-0005-0005-000000000005"), "E", new DateTime(2026, 4, 12, 9, 55, 0, 0, DateTimeKind.Utc), "Sylhet Tower 5", "Active", 11, new DateTime(2026, 4, 12, 9, 55, 0, 0, DateTimeKind.Utc) },
                    { new Guid("22222222-0006-0006-0006-000000000006"), "Building 6, Dhaka Residential Area 6", new Guid("11111111-0006-0006-0006-000000000006"), "A", new DateTime(2026, 4, 12, 9, 56, 0, 0, DateTimeKind.Utc), "Dhaka Tower 6", "Active", 12, new DateTime(2026, 4, 12, 9, 56, 0, 0, DateTimeKind.Utc) },
                    { new Guid("22222222-0007-0007-0007-000000000007"), "Building 7, Chattogram Residential Area 7", new Guid("11111111-0007-0007-0007-000000000007"), "B", new DateTime(2026, 4, 12, 9, 57, 0, 0, DateTimeKind.Utc), "Chattogram Tower 7", "Active", 13, new DateTime(2026, 4, 12, 9, 57, 0, 0, DateTimeKind.Utc) },
                    { new Guid("22222222-0008-0008-0008-000000000008"), "Building 8, Khulna Residential Area 8", new Guid("11111111-0008-0008-0008-000000000008"), "C", new DateTime(2026, 4, 12, 9, 58, 0, 0, DateTimeKind.Utc), "Khulna Tower 8", "Active", 14, new DateTime(2026, 4, 12, 9, 58, 0, 0, DateTimeKind.Utc) },
                    { new Guid("22222222-0009-0009-0009-000000000009"), "Building 9, Rajshahi Residential Area 9", new Guid("11111111-0009-0009-0009-000000000009"), "D", new DateTime(2026, 4, 12, 9, 59, 0, 0, DateTimeKind.Utc), "Rajshahi Tower 9", "Active", 15, new DateTime(2026, 4, 12, 9, 59, 0, 0, DateTimeKind.Utc) },
                    { new Guid("22222222-0010-0010-0010-000000000010"), "Building 10, Sylhet Residential Area 10", new Guid("11111111-0010-0010-0010-000000000010"), "E", new DateTime(2026, 4, 12, 10, 0, 0, 0, DateTimeKind.Utc), "Sylhet Tower 10", "Active", 6, new DateTime(2026, 4, 12, 10, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("22222222-0011-0011-0011-000000000011"), "Building 11, Dhaka Residential Area 11", new Guid("11111111-0011-0011-0011-000000000011"), "A", new DateTime(2026, 4, 12, 10, 1, 0, 0, DateTimeKind.Utc), "Dhaka Tower 11", "Active", 7, new DateTime(2026, 4, 12, 10, 1, 0, 0, DateTimeKind.Utc) },
                    { new Guid("22222222-0012-0012-0012-000000000012"), "Building 12, Chattogram Residential Area 12", new Guid("11111111-0012-0012-0012-000000000012"), "B", new DateTime(2026, 4, 12, 10, 2, 0, 0, DateTimeKind.Utc), "Chattogram Tower 12", "Active", 8, new DateTime(2026, 4, 12, 10, 2, 0, 0, DateTimeKind.Utc) },
                    { new Guid("22222222-0013-0013-0013-000000000013"), "Building 13, Khulna Residential Area 13", new Guid("11111111-0013-0013-0013-000000000013"), "C", new DateTime(2026, 4, 12, 10, 3, 0, 0, DateTimeKind.Utc), "Khulna Tower 13", "Active", 9, new DateTime(2026, 4, 12, 10, 3, 0, 0, DateTimeKind.Utc) },
                    { new Guid("22222222-0014-0014-0014-000000000014"), "Building 14, Rajshahi Residential Area 14", new Guid("11111111-0014-0014-0014-000000000014"), "D", new DateTime(2026, 4, 12, 10, 4, 0, 0, DateTimeKind.Utc), "Rajshahi Tower 14", "Active", 10, new DateTime(2026, 4, 12, 10, 4, 0, 0, DateTimeKind.Utc) },
                    { new Guid("22222222-0015-0015-0015-000000000015"), "Building 15, Sylhet Residential Area 15", new Guid("11111111-0015-0015-0015-000000000015"), "E", new DateTime(2026, 4, 12, 10, 5, 0, 0, DateTimeKind.Utc), "Sylhet Tower 15", "Active", 11, new DateTime(2026, 4, 12, 10, 5, 0, 0, DateTimeKind.Utc) },
                    { new Guid("22222222-0016-0016-0016-000000000016"), "Building 16, Dhaka Residential Area 16", new Guid("11111111-0016-0016-0016-000000000016"), "A", new DateTime(2026, 4, 12, 10, 6, 0, 0, DateTimeKind.Utc), "Dhaka Tower 16", "Active", 12, new DateTime(2026, 4, 12, 10, 6, 0, 0, DateTimeKind.Utc) },
                    { new Guid("22222222-0017-0017-0017-000000000017"), "Building 17, Chattogram Residential Area 17", new Guid("11111111-0017-0017-0017-000000000017"), "B", new DateTime(2026, 4, 12, 10, 7, 0, 0, DateTimeKind.Utc), "Chattogram Tower 17", "Active", 13, new DateTime(2026, 4, 12, 10, 7, 0, 0, DateTimeKind.Utc) },
                    { new Guid("22222222-0018-0018-0018-000000000018"), "Building 18, Khulna Residential Area 18", new Guid("11111111-0018-0018-0018-000000000018"), "C", new DateTime(2026, 4, 12, 10, 8, 0, 0, DateTimeKind.Utc), "Khulna Tower 18", "Active", 14, new DateTime(2026, 4, 12, 10, 8, 0, 0, DateTimeKind.Utc) },
                    { new Guid("22222222-0019-0019-0019-000000000019"), "Building 19, Rajshahi Residential Area 19", new Guid("11111111-0019-0019-0019-000000000019"), "D", new DateTime(2026, 4, 12, 10, 9, 0, 0, DateTimeKind.Utc), "Rajshahi Tower 19", "Active", 15, new DateTime(2026, 4, 12, 10, 9, 0, 0, DateTimeKind.Utc) },
                    { new Guid("22222222-0020-0020-0020-000000000020"), "Building 20, Sylhet Residential Area 20", new Guid("11111111-0020-0020-0020-000000000020"), "E", new DateTime(2026, 4, 12, 10, 10, 0, 0, DateTimeKind.Utc), "Sylhet Tower 20", "Active", 6, new DateTime(2026, 4, 12, 10, 10, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "Facilities",
                columns: new[] { "Id", "AreaId", "BookingRequired", "BuildingId", "Capacity", "CreatedAt", "FacilityType", "HourlyRate", "Name", "Rules", "Status", "UnitId", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("44444444-0001-0001-0001-000000000001"), new Guid("11111111-0001-0001-0001-000000000001"), false, null, 25, new DateTime(2026, 4, 12, 11, 31, 0, 0, DateTimeKind.Utc), "Gym", null, "Dhaka Gym 1", "{\"open\": \"06:00\", \"close\": \"22:00\", \"notes\": [\"Keep clean\", \"Follow community rules\", \"Facility no. 1\"]}", "Active", null, new DateTime(2026, 4, 12, 11, 31, 0, 0, DateTimeKind.Utc) },
                    { new Guid("44444444-0002-0002-0002-000000000002"), new Guid("11111111-0002-0002-0002-000000000002"), true, null, 30, new DateTime(2026, 4, 12, 11, 32, 0, 0, DateTimeKind.Utc), "Pool", 200m, "Chattogram Pool 2", "{\"open\": \"06:00\", \"close\": \"22:00\", \"notes\": [\"Keep clean\", \"Follow community rules\", \"Facility no. 2\"]}", "Active", null, new DateTime(2026, 4, 12, 11, 32, 0, 0, DateTimeKind.Utc) },
                    { new Guid("44444444-0003-0003-0003-000000000003"), new Guid("11111111-0003-0003-0003-000000000003"), false, null, 35, new DateTime(2026, 4, 12, 11, 33, 0, 0, DateTimeKind.Utc), "Playground", null, "Khulna Playground 3", "{\"open\": \"06:00\", \"close\": \"22:00\", \"notes\": [\"Keep clean\", \"Follow community rules\", \"Facility no. 3\"]}", "Active", null, new DateTime(2026, 4, 12, 11, 33, 0, 0, DateTimeKind.Utc) },
                    { new Guid("44444444-0004-0004-0004-000000000004"), new Guid("11111111-0004-0004-0004-000000000004"), true, null, 40, new DateTime(2026, 4, 12, 11, 34, 0, 0, DateTimeKind.Utc), "Hall", 250m, "Rajshahi Hall 4", "{\"open\": \"06:00\", \"close\": \"22:00\", \"notes\": [\"Keep clean\", \"Follow community rules\", \"Facility no. 4\"]}", "Active", null, new DateTime(2026, 4, 12, 11, 34, 0, 0, DateTimeKind.Utc) },
                    { new Guid("44444444-0005-0005-0005-000000000005"), new Guid("11111111-0005-0005-0005-000000000005"), false, null, 45, new DateTime(2026, 4, 12, 11, 35, 0, 0, DateTimeKind.Utc), "Mosque", null, "Sylhet Mosque 5", "{\"open\": \"06:00\", \"close\": \"22:00\", \"notes\": [\"Keep clean\", \"Follow community rules\", \"Facility no. 5\"]}", "Active", null, new DateTime(2026, 4, 12, 11, 35, 0, 0, DateTimeKind.Utc) },
                    { new Guid("44444444-0006-0006-0006-000000000006"), new Guid("11111111-0006-0006-0006-000000000006"), true, null, 50, new DateTime(2026, 4, 12, 11, 36, 0, 0, DateTimeKind.Utc), "Library", 300m, "Dhaka Library 6", "{\"open\": \"06:00\", \"close\": \"22:00\", \"notes\": [\"Keep clean\", \"Follow community rules\", \"Facility no. 6\"]}", "Active", null, new DateTime(2026, 4, 12, 11, 36, 0, 0, DateTimeKind.Utc) },
                    { new Guid("44444444-0007-0007-0007-000000000007"), new Guid("11111111-0007-0007-0007-000000000007"), false, null, 55, new DateTime(2026, 4, 12, 11, 37, 0, 0, DateTimeKind.Utc), "Laundry", null, "Chattogram Laundry 7", "{\"open\": \"06:00\", \"close\": \"22:00\", \"notes\": [\"Keep clean\", \"Follow community rules\", \"Facility no. 7\"]}", "Active", null, new DateTime(2026, 4, 12, 11, 37, 0, 0, DateTimeKind.Utc) },
                    { new Guid("44444444-0008-0008-0008-000000000008"), new Guid("11111111-0008-0008-0008-000000000008"), true, null, 60, new DateTime(2026, 4, 12, 11, 38, 0, 0, DateTimeKind.Utc), "Garden", 350m, "Khulna Garden 8", "{\"open\": \"06:00\", \"close\": \"22:00\", \"notes\": [\"Keep clean\", \"Follow community rules\", \"Facility no. 8\"]}", "Active", null, new DateTime(2026, 4, 12, 11, 38, 0, 0, DateTimeKind.Utc) },
                    { new Guid("44444444-0009-0009-0009-000000000009"), new Guid("11111111-0009-0009-0009-000000000009"), false, null, 65, new DateTime(2026, 4, 12, 11, 39, 0, 0, DateTimeKind.Utc), "Clinic", null, "Rajshahi Clinic 9", "{\"open\": \"06:00\", \"close\": \"22:00\", \"notes\": [\"Keep clean\", \"Follow community rules\", \"Facility no. 9\"]}", "Active", null, new DateTime(2026, 4, 12, 11, 39, 0, 0, DateTimeKind.Utc) },
                    { new Guid("44444444-0010-0010-0010-000000000010"), new Guid("11111111-0010-0010-0010-000000000010"), true, null, 70, new DateTime(2026, 4, 12, 11, 40, 0, 0, DateTimeKind.Utc), "Cafeteria", 400m, "Sylhet Cafeteria 10", "{\"open\": \"06:00\", \"close\": \"22:00\", \"notes\": [\"Keep clean\", \"Follow community rules\", \"Facility no. 10\"]}", "Active", null, new DateTime(2026, 4, 12, 11, 40, 0, 0, DateTimeKind.Utc) },
                    { new Guid("44444444-0011-0011-0011-000000000011"), null, true, new Guid("22222222-0011-0011-0011-000000000011"), 69, new DateTime(2026, 4, 12, 11, 41, 0, 0, DateTimeKind.Utc), "Gym", 420m, "Tower Facility 11", "{\"open\": \"07:00\", \"close\": \"23:00\", \"notes\": [\"Booking may be required\", \"Residents only\", \"Building linked facility 11\"]}", "Active", null, new DateTime(2026, 4, 12, 11, 41, 0, 0, DateTimeKind.Utc) },
                    { new Guid("44444444-0012-0012-0012-000000000012"), null, false, new Guid("22222222-0012-0012-0012-000000000012"), 73, new DateTime(2026, 4, 12, 11, 42, 0, 0, DateTimeKind.Utc), "Pool", null, "Tower Facility 12", "{\"open\": \"07:00\", \"close\": \"23:00\", \"notes\": [\"Booking may be required\", \"Residents only\", \"Building linked facility 12\"]}", "Active", null, new DateTime(2026, 4, 12, 11, 42, 0, 0, DateTimeKind.Utc) },
                    { new Guid("44444444-0013-0013-0013-000000000013"), null, true, new Guid("22222222-0013-0013-0013-000000000013"), 77, new DateTime(2026, 4, 12, 11, 43, 0, 0, DateTimeKind.Utc), "Playground", 460m, "Tower Facility 13", "{\"open\": \"07:00\", \"close\": \"23:00\", \"notes\": [\"Booking may be required\", \"Residents only\", \"Building linked facility 13\"]}", "Active", null, new DateTime(2026, 4, 12, 11, 43, 0, 0, DateTimeKind.Utc) },
                    { new Guid("44444444-0014-0014-0014-000000000014"), null, false, new Guid("22222222-0014-0014-0014-000000000014"), 81, new DateTime(2026, 4, 12, 11, 44, 0, 0, DateTimeKind.Utc), "Hall", null, "Tower Facility 14", "{\"open\": \"07:00\", \"close\": \"23:00\", \"notes\": [\"Booking may be required\", \"Residents only\", \"Building linked facility 14\"]}", "Active", null, new DateTime(2026, 4, 12, 11, 44, 0, 0, DateTimeKind.Utc) },
                    { new Guid("44444444-0015-0015-0015-000000000015"), null, true, new Guid("22222222-0015-0015-0015-000000000015"), 85, new DateTime(2026, 4, 12, 11, 45, 0, 0, DateTimeKind.Utc), "Mosque", 500m, "Tower Facility 15", "{\"open\": \"07:00\", \"close\": \"23:00\", \"notes\": [\"Booking may be required\", \"Residents only\", \"Building linked facility 15\"]}", "Active", null, new DateTime(2026, 4, 12, 11, 45, 0, 0, DateTimeKind.Utc) },
                    { new Guid("44444444-0016-0016-0016-000000000016"), null, false, new Guid("22222222-0016-0016-0016-000000000016"), 89, new DateTime(2026, 4, 12, 11, 46, 0, 0, DateTimeKind.Utc), "Library", null, "Tower Facility 16", "{\"open\": \"07:00\", \"close\": \"23:00\", \"notes\": [\"Booking may be required\", \"Residents only\", \"Building linked facility 16\"]}", "Active", null, new DateTime(2026, 4, 12, 11, 46, 0, 0, DateTimeKind.Utc) },
                    { new Guid("44444444-0017-0017-0017-000000000017"), null, true, new Guid("22222222-0017-0017-0017-000000000017"), 93, new DateTime(2026, 4, 12, 11, 47, 0, 0, DateTimeKind.Utc), "Laundry", 540m, "Tower Facility 17", "{\"open\": \"07:00\", \"close\": \"23:00\", \"notes\": [\"Booking may be required\", \"Residents only\", \"Building linked facility 17\"]}", "Active", null, new DateTime(2026, 4, 12, 11, 47, 0, 0, DateTimeKind.Utc) },
                    { new Guid("44444444-0018-0018-0018-000000000018"), null, false, new Guid("22222222-0018-0018-0018-000000000018"), 97, new DateTime(2026, 4, 12, 11, 48, 0, 0, DateTimeKind.Utc), "Garden", null, "Tower Facility 18", "{\"open\": \"07:00\", \"close\": \"23:00\", \"notes\": [\"Booking may be required\", \"Residents only\", \"Building linked facility 18\"]}", "Active", null, new DateTime(2026, 4, 12, 11, 48, 0, 0, DateTimeKind.Utc) },
                    { new Guid("44444444-0019-0019-0019-000000000019"), null, true, new Guid("22222222-0019-0019-0019-000000000019"), 101, new DateTime(2026, 4, 12, 11, 49, 0, 0, DateTimeKind.Utc), "Clinic", 580m, "Tower Facility 19", "{\"open\": \"07:00\", \"close\": \"23:00\", \"notes\": [\"Booking may be required\", \"Residents only\", \"Building linked facility 19\"]}", "Active", null, new DateTime(2026, 4, 12, 11, 49, 0, 0, DateTimeKind.Utc) },
                    { new Guid("44444444-0020-0020-0020-000000000020"), null, false, new Guid("22222222-0020-0020-0020-000000000020"), 105, new DateTime(2026, 4, 12, 11, 50, 0, 0, DateTimeKind.Utc), "Cafeteria", null, "Tower Facility 20", "{\"open\": \"07:00\", \"close\": \"23:00\", \"notes\": [\"Booking may be required\", \"Residents only\", \"Building linked facility 20\"]}", "Active", null, new DateTime(2026, 4, 12, 11, 50, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "Units",
                columns: new[] { "Id", "AreaSqft", "Bathrooms", "Bedrooms", "BuildingId", "CreatedAt", "CurrentLeaseId", "FloorNo", "OccupancyStatus", "OwnershipType", "UnitNo", "UnitType", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("33333333-0001-0001-0001-000000000001"), 822.5m, 2, 2, new Guid("22222222-0001-0001-0001-000000000001"), new DateTime(2026, 4, 12, 10, 41, 0, 0, DateTimeKind.Utc), null, 2, "Vacant", "Owned", "A-02", "Apartment", new DateTime(2026, 4, 12, 10, 41, 0, 0, DateTimeKind.Utc) },
                    { new Guid("33333333-0002-0002-0002-000000000002"), 865.0m, 3, 3, new Guid("22222222-0002-0002-0002-000000000002"), new DateTime(2026, 4, 12, 10, 42, 0, 0, DateTimeKind.Utc), null, 3, "Occupied", "Rented", "B-03", "Shop", new DateTime(2026, 4, 12, 10, 42, 0, 0, DateTimeKind.Utc) },
                    { new Guid("33333333-0003-0003-0003-000000000003"), 907.5m, 1, 4, new Guid("22222222-0003-0003-0003-000000000003"), new DateTime(2026, 4, 12, 10, 43, 0, 0, DateTimeKind.Utc), null, 4, "Reserved", "Association", "C-04", "Office", new DateTime(2026, 4, 12, 10, 43, 0, 0, DateTimeKind.Utc) },
                    { new Guid("33333333-0004-0004-0004-000000000004"), 950.0m, 2, 1, new Guid("22222222-0004-0004-0004-000000000004"), new DateTime(2026, 4, 12, 10, 44, 0, 0, DateTimeKind.Utc), null, 5, "Maintenance", "Owned", "D-05", "Storage", new DateTime(2026, 4, 12, 10, 44, 0, 0, DateTimeKind.Utc) },
                    { new Guid("33333333-0005-0005-0005-000000000005"), 992.5m, 3, 2, new Guid("22222222-0005-0005-0005-000000000005"), new DateTime(2026, 4, 12, 10, 45, 0, 0, DateTimeKind.Utc), null, 6, "Vacant", "Rented", "E-06", "Apartment", new DateTime(2026, 4, 12, 10, 45, 0, 0, DateTimeKind.Utc) },
                    { new Guid("33333333-0006-0006-0006-000000000006"), 1035.0m, 1, 3, new Guid("22222222-0006-0006-0006-000000000006"), new DateTime(2026, 4, 12, 10, 46, 0, 0, DateTimeKind.Utc), null, 7, "Occupied", "Association", "A-07", "Shop", new DateTime(2026, 4, 12, 10, 46, 0, 0, DateTimeKind.Utc) },
                    { new Guid("33333333-0007-0007-0007-000000000007"), 1077.5m, 2, 4, new Guid("22222222-0007-0007-0007-000000000007"), new DateTime(2026, 4, 12, 10, 47, 0, 0, DateTimeKind.Utc), null, 8, "Reserved", "Owned", "B-08", "Office", new DateTime(2026, 4, 12, 10, 47, 0, 0, DateTimeKind.Utc) },
                    { new Guid("33333333-0008-0008-0008-000000000008"), 1120.0m, 3, 1, new Guid("22222222-0008-0008-0008-000000000008"), new DateTime(2026, 4, 12, 10, 48, 0, 0, DateTimeKind.Utc), null, 9, "Maintenance", "Rented", "C-09", "Storage", new DateTime(2026, 4, 12, 10, 48, 0, 0, DateTimeKind.Utc) },
                    { new Guid("33333333-0009-0009-0009-000000000009"), 1162.5m, 1, 2, new Guid("22222222-0009-0009-0009-000000000009"), new DateTime(2026, 4, 12, 10, 49, 0, 0, DateTimeKind.Utc), null, 10, "Vacant", "Association", "D-10", "Apartment", new DateTime(2026, 4, 12, 10, 49, 0, 0, DateTimeKind.Utc) },
                    { new Guid("33333333-0010-0010-0010-000000000010"), 1205.0m, 2, 3, new Guid("22222222-0010-0010-0010-000000000010"), new DateTime(2026, 4, 12, 10, 50, 0, 0, DateTimeKind.Utc), null, 11, "Occupied", "Owned", "E-11", "Shop", new DateTime(2026, 4, 12, 10, 50, 0, 0, DateTimeKind.Utc) },
                    { new Guid("33333333-0011-0011-0011-000000000011"), 1247.5m, 3, 4, new Guid("22222222-0011-0011-0011-000000000011"), new DateTime(2026, 4, 12, 10, 51, 0, 0, DateTimeKind.Utc), null, 12, "Reserved", "Rented", "A-12", "Office", new DateTime(2026, 4, 12, 10, 51, 0, 0, DateTimeKind.Utc) },
                    { new Guid("33333333-0012-0012-0012-000000000012"), 1290.0m, 1, 1, new Guid("22222222-0012-0012-0012-000000000012"), new DateTime(2026, 4, 12, 10, 52, 0, 0, DateTimeKind.Utc), null, 1, "Maintenance", "Association", "B-01", "Storage", new DateTime(2026, 4, 12, 10, 52, 0, 0, DateTimeKind.Utc) },
                    { new Guid("33333333-0013-0013-0013-000000000013"), 1332.5m, 2, 2, new Guid("22222222-0013-0013-0013-000000000013"), new DateTime(2026, 4, 12, 10, 53, 0, 0, DateTimeKind.Utc), null, 2, "Vacant", "Owned", "C-02", "Apartment", new DateTime(2026, 4, 12, 10, 53, 0, 0, DateTimeKind.Utc) },
                    { new Guid("33333333-0014-0014-0014-000000000014"), 1375.0m, 3, 3, new Guid("22222222-0014-0014-0014-000000000014"), new DateTime(2026, 4, 12, 10, 54, 0, 0, DateTimeKind.Utc), null, 3, "Occupied", "Rented", "D-03", "Shop", new DateTime(2026, 4, 12, 10, 54, 0, 0, DateTimeKind.Utc) },
                    { new Guid("33333333-0015-0015-0015-000000000015"), 1417.5m, 1, 4, new Guid("22222222-0015-0015-0015-000000000015"), new DateTime(2026, 4, 12, 10, 55, 0, 0, DateTimeKind.Utc), null, 4, "Reserved", "Association", "E-04", "Office", new DateTime(2026, 4, 12, 10, 55, 0, 0, DateTimeKind.Utc) },
                    { new Guid("33333333-0016-0016-0016-000000000016"), 1460.0m, 2, 1, new Guid("22222222-0016-0016-0016-000000000016"), new DateTime(2026, 4, 12, 10, 56, 0, 0, DateTimeKind.Utc), null, 5, "Maintenance", "Owned", "A-05", "Storage", new DateTime(2026, 4, 12, 10, 56, 0, 0, DateTimeKind.Utc) },
                    { new Guid("33333333-0017-0017-0017-000000000017"), 1502.5m, 3, 2, new Guid("22222222-0017-0017-0017-000000000017"), new DateTime(2026, 4, 12, 10, 57, 0, 0, DateTimeKind.Utc), null, 6, "Vacant", "Rented", "B-06", "Apartment", new DateTime(2026, 4, 12, 10, 57, 0, 0, DateTimeKind.Utc) },
                    { new Guid("33333333-0018-0018-0018-000000000018"), 1545.0m, 1, 3, new Guid("22222222-0018-0018-0018-000000000018"), new DateTime(2026, 4, 12, 10, 58, 0, 0, DateTimeKind.Utc), null, 7, "Occupied", "Association", "C-07", "Shop", new DateTime(2026, 4, 12, 10, 58, 0, 0, DateTimeKind.Utc) },
                    { new Guid("33333333-0019-0019-0019-000000000019"), 1587.5m, 2, 4, new Guid("22222222-0019-0019-0019-000000000019"), new DateTime(2026, 4, 12, 10, 59, 0, 0, DateTimeKind.Utc), null, 8, "Reserved", "Owned", "D-08", "Office", new DateTime(2026, 4, 12, 10, 59, 0, 0, DateTimeKind.Utc) },
                    { new Guid("33333333-0020-0020-0020-000000000020"), 1630.0m, 3, 1, new Guid("22222222-0020-0020-0020-000000000020"), new DateTime(2026, 4, 12, 11, 0, 0, 0, DateTimeKind.Utc), null, 9, "Maintenance", "Rented", "E-09", "Storage", new DateTime(2026, 4, 12, 11, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "ParkingSlots",
                columns: new[] { "Id", "AssignedResidentId", "BuildingId", "CreatedAt", "SlotType", "Status", "UnitId", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("66666666-0001-0001-0001-000000000001"), null, new Guid("22222222-0001-0001-0001-000000000001"), new DateTime(2026, 4, 12, 12, 21, 0, 0, DateTimeKind.Utc), "Compact", "Active", new Guid("33333333-0001-0001-0001-000000000001"), new DateTime(2026, 4, 12, 12, 21, 0, 0, DateTimeKind.Utc) },
                    { new Guid("66666666-0002-0002-0002-000000000002"), null, new Guid("22222222-0002-0002-0002-000000000002"), new DateTime(2026, 4, 12, 12, 22, 0, 0, DateTimeKind.Utc), "Compact", "Active", new Guid("33333333-0002-0002-0002-000000000002"), new DateTime(2026, 4, 12, 12, 22, 0, 0, DateTimeKind.Utc) },
                    { new Guid("66666666-0003-0003-0003-000000000003"), null, new Guid("22222222-0003-0003-0003-000000000003"), new DateTime(2026, 4, 12, 12, 23, 0, 0, DateTimeKind.Utc), "Compact", "Active", new Guid("33333333-0003-0003-0003-000000000003"), new DateTime(2026, 4, 12, 12, 23, 0, 0, DateTimeKind.Utc) },
                    { new Guid("66666666-0004-0004-0004-000000000004"), null, new Guid("22222222-0004-0004-0004-000000000004"), new DateTime(2026, 4, 12, 12, 24, 0, 0, DateTimeKind.Utc), "Compact", "Active", new Guid("33333333-0004-0004-0004-000000000004"), new DateTime(2026, 4, 12, 12, 24, 0, 0, DateTimeKind.Utc) },
                    { new Guid("66666666-0005-0005-0005-000000000005"), null, new Guid("22222222-0005-0005-0005-000000000005"), new DateTime(2026, 4, 12, 12, 25, 0, 0, DateTimeKind.Utc), "Compact", "Active", new Guid("33333333-0005-0005-0005-000000000005"), new DateTime(2026, 4, 12, 12, 25, 0, 0, DateTimeKind.Utc) },
                    { new Guid("66666666-0006-0006-0006-000000000006"), null, new Guid("22222222-0006-0006-0006-000000000006"), new DateTime(2026, 4, 12, 12, 26, 0, 0, DateTimeKind.Utc), "Compact", "Active", new Guid("33333333-0006-0006-0006-000000000006"), new DateTime(2026, 4, 12, 12, 26, 0, 0, DateTimeKind.Utc) },
                    { new Guid("66666666-0007-0007-0007-000000000007"), null, new Guid("22222222-0007-0007-0007-000000000007"), new DateTime(2026, 4, 12, 12, 27, 0, 0, DateTimeKind.Utc), "Compact", "Active", new Guid("33333333-0007-0007-0007-000000000007"), new DateTime(2026, 4, 12, 12, 27, 0, 0, DateTimeKind.Utc) },
                    { new Guid("66666666-0008-0008-0008-000000000008"), null, new Guid("22222222-0008-0008-0008-000000000008"), new DateTime(2026, 4, 12, 12, 28, 0, 0, DateTimeKind.Utc), "Compact", "Active", new Guid("33333333-0008-0008-0008-000000000008"), new DateTime(2026, 4, 12, 12, 28, 0, 0, DateTimeKind.Utc) },
                    { new Guid("66666666-0009-0009-0009-000000000009"), null, new Guid("22222222-0009-0009-0009-000000000009"), new DateTime(2026, 4, 12, 12, 29, 0, 0, DateTimeKind.Utc), "Compact", "Active", new Guid("33333333-0009-0009-0009-000000000009"), new DateTime(2026, 4, 12, 12, 29, 0, 0, DateTimeKind.Utc) },
                    { new Guid("66666666-0010-0010-0010-000000000010"), null, new Guid("22222222-0010-0010-0010-000000000010"), new DateTime(2026, 4, 12, 12, 30, 0, 0, DateTimeKind.Utc), "Compact", "Active", new Guid("33333333-0010-0010-0010-000000000010"), new DateTime(2026, 4, 12, 12, 30, 0, 0, DateTimeKind.Utc) },
                    { new Guid("66666666-0011-0011-0011-000000000011"), null, new Guid("22222222-0011-0011-0011-000000000011"), new DateTime(2026, 4, 12, 12, 31, 0, 0, DateTimeKind.Utc), "Compact", "Active", new Guid("33333333-0011-0011-0011-000000000011"), new DateTime(2026, 4, 12, 12, 31, 0, 0, DateTimeKind.Utc) },
                    { new Guid("66666666-0012-0012-0012-000000000012"), null, new Guid("22222222-0012-0012-0012-000000000012"), new DateTime(2026, 4, 12, 12, 32, 0, 0, DateTimeKind.Utc), "Compact", "Active", new Guid("33333333-0012-0012-0012-000000000012"), new DateTime(2026, 4, 12, 12, 32, 0, 0, DateTimeKind.Utc) },
                    { new Guid("66666666-0013-0013-0013-000000000013"), null, new Guid("22222222-0013-0013-0013-000000000013"), new DateTime(2026, 4, 12, 12, 33, 0, 0, DateTimeKind.Utc), "Compact", "Active", new Guid("33333333-0013-0013-0013-000000000013"), new DateTime(2026, 4, 12, 12, 33, 0, 0, DateTimeKind.Utc) },
                    { new Guid("66666666-0014-0014-0014-000000000014"), null, new Guid("22222222-0014-0014-0014-000000000014"), new DateTime(2026, 4, 12, 12, 34, 0, 0, DateTimeKind.Utc), "Compact", "Active", new Guid("33333333-0014-0014-0014-000000000014"), new DateTime(2026, 4, 12, 12, 34, 0, 0, DateTimeKind.Utc) },
                    { new Guid("66666666-0015-0015-0015-000000000015"), null, new Guid("22222222-0015-0015-0015-000000000015"), new DateTime(2026, 4, 12, 12, 35, 0, 0, DateTimeKind.Utc), "Compact", "Active", new Guid("33333333-0015-0015-0015-000000000015"), new DateTime(2026, 4, 12, 12, 35, 0, 0, DateTimeKind.Utc) },
                    { new Guid("66666666-0016-0016-0016-000000000016"), null, new Guid("22222222-0016-0016-0016-000000000016"), new DateTime(2026, 4, 12, 12, 36, 0, 0, DateTimeKind.Utc), "Compact", "Active", new Guid("33333333-0016-0016-0016-000000000016"), new DateTime(2026, 4, 12, 12, 36, 0, 0, DateTimeKind.Utc) },
                    { new Guid("66666666-0017-0017-0017-000000000017"), null, new Guid("22222222-0017-0017-0017-000000000017"), new DateTime(2026, 4, 12, 12, 37, 0, 0, DateTimeKind.Utc), "Compact", "Active", new Guid("33333333-0017-0017-0017-000000000017"), new DateTime(2026, 4, 12, 12, 37, 0, 0, DateTimeKind.Utc) },
                    { new Guid("66666666-0018-0018-0018-000000000018"), null, new Guid("22222222-0018-0018-0018-000000000018"), new DateTime(2026, 4, 12, 12, 38, 0, 0, DateTimeKind.Utc), "Compact", "Active", new Guid("33333333-0018-0018-0018-000000000018"), new DateTime(2026, 4, 12, 12, 38, 0, 0, DateTimeKind.Utc) },
                    { new Guid("66666666-0019-0019-0019-000000000019"), null, new Guid("22222222-0019-0019-0019-000000000019"), new DateTime(2026, 4, 12, 12, 39, 0, 0, DateTimeKind.Utc), "Compact", "Active", new Guid("33333333-0019-0019-0019-000000000019"), new DateTime(2026, 4, 12, 12, 39, 0, 0, DateTimeKind.Utc) },
                    { new Guid("66666666-0020-0020-0020-000000000020"), null, new Guid("22222222-0020-0020-0020-000000000020"), new DateTime(2026, 4, 12, 12, 40, 0, 0, DateTimeKind.Utc), "Compact", "Active", new Guid("33333333-0020-0020-0020-000000000020"), new DateTime(2026, 4, 12, 12, 40, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Areas_City_State",
                table: "Areas",
                columns: new[] { "City", "State" });

            migrationBuilder.CreateIndex(
                name: "IX_Areas_Code",
                table: "Areas",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Areas_Name",
                table: "Areas",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Buildings_AreaId",
                table: "Buildings",
                column: "AreaId");

            migrationBuilder.CreateIndex(
                name: "IX_Buildings_Code",
                table: "Buildings",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Facilities_AreaId",
                table: "Facilities",
                column: "AreaId");

            migrationBuilder.CreateIndex(
                name: "IX_Facilities_BuildingId",
                table: "Facilities",
                column: "BuildingId");

            migrationBuilder.CreateIndex(
                name: "IX_Facilities_UnitId",
                table: "Facilities",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_Images_Code",
                table: "Images",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_ParkingSlots_AssignedResidentId",
                table: "ParkingSlots",
                column: "AssignedResidentId");

            migrationBuilder.CreateIndex(
                name: "IX_ParkingSlots_BuildingId",
                table: "ParkingSlots",
                column: "BuildingId");

            migrationBuilder.CreateIndex(
                name: "IX_ParkingSlots_SlotCode",
                table: "ParkingSlots",
                column: "SlotCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ParkingSlots_UnitId",
                table: "ParkingSlots",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_Units_BuildingId_UnitNo",
                table: "Units",
                columns: new[] { "BuildingId", "UnitNo" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Facilities");

            migrationBuilder.DropTable(
                name: "Images");

            migrationBuilder.DropTable(
                name: "ParkingSlots");

            migrationBuilder.DropTable(
                name: "Units");

            migrationBuilder.DropTable(
                name: "Buildings");

            migrationBuilder.DropTable(
                name: "Areas");
        }
    }
}
