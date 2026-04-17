using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAutomationFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DataEstimatedValue",
                table: "Volunteers",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DeduplicationScore",
                table: "Volunteers",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastUploadAttempt",
                table: "Volunteers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UploadAttempts",
                table: "Volunteers",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "AnalysisResult",
                table: "Datasets",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DataEstimatedValue",
                table: "Volunteers");

            migrationBuilder.DropColumn(
                name: "DeduplicationScore",
                table: "Volunteers");

            migrationBuilder.DropColumn(
                name: "LastUploadAttempt",
                table: "Volunteers");

            migrationBuilder.DropColumn(
                name: "UploadAttempts",
                table: "Volunteers");

            migrationBuilder.DropColumn(
                name: "AnalysisResult",
                table: "Datasets");
        }
    }
}
