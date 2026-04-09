using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebWikiForum.Migrations
{
    /// <inheritdoc />
    public partial class AddAgencyCommunityLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {


            migrationBuilder.AddColumn<string>(
                name: "TwitterUrl",
                table: "Agencies",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WebsiteUrl",
                table: "Agencies",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "YoutubeUrl",
                table: "Agencies",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {


            migrationBuilder.DropColumn(
                name: "TwitterUrl",
                table: "Agencies");

            migrationBuilder.DropColumn(
                name: "WebsiteUrl",
                table: "Agencies");

            migrationBuilder.DropColumn(
                name: "YoutubeUrl",
                table: "Agencies");
        }
    }
}
