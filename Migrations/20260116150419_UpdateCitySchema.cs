using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TurkeyCityGuide.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCitySchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "AreaKm2",
                table: "Cities",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "DistrictCount",
                table: "Cities",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "DistrictMapImage",
                table: "Cities",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Elevation",
                table: "Cities",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PlateCode",
                table: "Cities",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Population",
                table: "Cities",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Region",
                table: "Cities",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AreaKm2",
                table: "Cities");

            migrationBuilder.DropColumn(
                name: "DistrictCount",
                table: "Cities");

            migrationBuilder.DropColumn(
                name: "DistrictMapImage",
                table: "Cities");

            migrationBuilder.DropColumn(
                name: "Elevation",
                table: "Cities");

            migrationBuilder.DropColumn(
                name: "PlateCode",
                table: "Cities");

            migrationBuilder.DropColumn(
                name: "Population",
                table: "Cities");

            migrationBuilder.DropColumn(
                name: "Region",
                table: "Cities");
        }
    }
}
