using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddB2BLicensingTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create DataPools table
            migrationBuilder.CreateTable(
                name: "DataPools",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Category = table.Column<string>(type: "TEXT", nullable: false),
                    PricePerMonth = table.Column<decimal>(type: "TEXT", nullable: false),
                    RevenueSharePercent = table.Column<decimal>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    Created = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: false),
                    LastModified = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastModifiedBY = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataPools", x => x.Id);
                });

            // Create ApiKeys table
            migrationBuilder.CreateTable(
                name: "ApiKeys",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    BuyerId = table.Column<string>(type: "TEXT", nullable: false),
                    KeyPrefix = table.Column<string>(type: "TEXT", nullable: false),
                    KeyHash = table.Column<string>(type: "TEXT", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsRevoked = table.Column<bool>(type: "INTEGER", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Created = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: false),
                    LastModified = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastModifiedBY = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiKeys", x => x.Id);
                });

            // Create DataLicenses table
            migrationBuilder.CreateTable(
                name: "DataLicenses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    BuyerId = table.Column<string>(type: "TEXT", nullable: false),
                    DataPoolId = table.Column<int>(type: "INTEGER", nullable: false),
                    ApiKeyId = table.Column<Guid>(type: "TEXT", nullable: false),
                    LicensedFrom = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LicensedUntil = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AmountPaid = table.Column<decimal>(type: "TEXT", nullable: false),
                    PlatformFee = table.Column<decimal>(type: "TEXT", nullable: false),
                    VolunteerShare = table.Column<decimal>(type: "TEXT", nullable: false),
                    MonthsLicensed = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    DistributionTxRef = table.Column<string>(type: "TEXT", nullable: true),
                    Created = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: false),
                    LastModified = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastModifiedBY = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataLicenses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DataLicenses_ApiKeys_ApiKeyId",
                        column: x => x.ApiKeyId,
                        principalTable: "ApiKeys",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DataLicenses_DataPools_DataPoolId",
                        column: x => x.DataPoolId,
                        principalTable: "DataPools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Add DataPoolId column to Datasets table (if not exists)
            migrationBuilder.AddColumn<int>(
                name: "DataPoolId",
                table: "Datasets",
                type: "INTEGER",
                nullable: true);

            // Create indexes
            migrationBuilder.CreateIndex(
                name: "IX_DataLicenses_ApiKeyId",
                table: "DataLicenses",
                column: "ApiKeyId");

            migrationBuilder.CreateIndex(
                name: "IX_DataLicenses_DataPoolId",
                table: "DataLicenses",
                column: "DataPoolId");

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeys_BuyerId",
                table: "ApiKeys",
                column: "BuyerId");

            migrationBuilder.CreateIndex(
                name: "IX_DataPools_Name",
                table: "DataPools",
                column: "Name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "DataLicenses");
            migrationBuilder.DropTable(name: "ApiKeys");
            migrationBuilder.DropTable(name: "DataPools");

            migrationBuilder.DropColumn(
                name: "DataPoolId",
                table: "Datasets");
        }
    }
}
