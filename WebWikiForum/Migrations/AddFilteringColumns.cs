using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebWikiForum.Migrations
{
    /// <inheritdoc />
    public partial class AddFilteringColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsIndependent",
                table: "Vtubers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Language",
                table: "Vtubers",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Region",
                table: "Vtubers",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Tags",
                table: "Vtubers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Agencies",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Focus",
                table: "Agencies",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Region",
                table: "Agencies",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "TalentCount",
                table: "Agencies",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsIndependent",
                table: "Vtubers");

            migrationBuilder.DropColumn(
                name: "Language",
                table: "Vtubers");

            migrationBuilder.DropColumn(
                name: "Region",
                table: "Vtubers");

            migrationBuilder.DropColumn(
                name: "Tags",
                table: "Vtubers");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Agencies");

            migrationBuilder.DropColumn(
                name: "Focus",
                table: "Agencies");

            migrationBuilder.DropColumn(
                name: "Region",
                table: "Agencies");

            migrationBuilder.DropColumn(
                name: "TalentCount",
                table: "Agencies");
        }
    }
}
