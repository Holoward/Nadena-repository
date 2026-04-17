using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIntegrityFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DataIntegrityHash",
                table: "Volunteers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IntegrityReason",
                table: "Volunteers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IntegrityStatus",
                table: "Volunteers",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "DataIntegrityHash",
                table: "Datasets",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IntegrityReason",
                table: "Datasets",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IntegrityStatus",
                table: "Datasets",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DataIntegrityHash",
                table: "Volunteers");

            migrationBuilder.DropColumn(
                name: "IntegrityReason",
                table: "Volunteers");

            migrationBuilder.DropColumn(
                name: "IntegrityStatus",
                table: "Volunteers");

            migrationBuilder.DropColumn(
                name: "DataIntegrityHash",
                table: "Datasets");

            migrationBuilder.DropColumn(
                name: "IntegrityReason",
                table: "Datasets");

            migrationBuilder.DropColumn(
                name: "IntegrityStatus",
                table: "Datasets");
        }
    }
}
