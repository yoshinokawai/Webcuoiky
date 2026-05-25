using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebWikiForum.Migrations
{
    /// <inheritdoc />
    public partial class AddNewSongToVtuber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NewSongTitle",
                table: "Vtubers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NewSongVideoUrl",
                table: "Vtubers",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NewSongTitle",
                table: "Vtubers");

            migrationBuilder.DropColumn(
                name: "NewSongVideoUrl",
                table: "Vtubers");
        }
    }
}
