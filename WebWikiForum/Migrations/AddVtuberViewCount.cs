using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebWikiForum.Migrations
{
    /// <inheritdoc />
    public partial class AddVtuberViewCount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ViewCount",
                table: "Vtubers",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ViewCount",
                table: "Vtubers");
        }
    }
}
