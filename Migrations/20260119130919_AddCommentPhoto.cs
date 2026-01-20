using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TurkeyCityGuide.Migrations
{
    /// <inheritdoc />
    public partial class AddCommentPhoto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Comments_Districts_DistrictId",
                table: "Comments");

            migrationBuilder.AddColumn<string>(
                name: "PhotoPath",
                table: "Comments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Comments_Districts_DistrictId",
                table: "Comments",
                column: "DistrictId",
                principalTable: "Districts",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Comments_Districts_DistrictId",
                table: "Comments");

            migrationBuilder.DropColumn(
                name: "PhotoPath",
                table: "Comments");

            migrationBuilder.AddForeignKey(
                name: "FK_Comments_Districts_DistrictId",
                table: "Comments",
                column: "DistrictId",
                principalTable: "Districts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
