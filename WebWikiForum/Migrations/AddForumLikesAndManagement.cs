using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebWikiForum.Migrations
{
    /// <inheritdoc />
    public partial class AddForumLikesAndManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LikeCount",
                table: "Discussions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LikeCount",
                table: "DiscussionReplies",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "DiscussionLikes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DiscussionId = table.Column<int>(type: "int", nullable: true),
                    ReplyId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscussionLikes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiscussionLikes_DiscussionReplies_ReplyId",
                        column: x => x.ReplyId,
                        principalTable: "DiscussionReplies",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DiscussionLikes_Discussions_DiscussionId",
                        column: x => x.DiscussionId,
                        principalTable: "Discussions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_DiscussionLikes_DiscussionId",
                table: "DiscussionLikes",
                column: "DiscussionId");

            migrationBuilder.CreateIndex(
                name: "IX_DiscussionLikes_ReplyId",
                table: "DiscussionLikes",
                column: "ReplyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DiscussionLikes");

            migrationBuilder.DropColumn(
                name: "LikeCount",
                table: "Discussions");

            migrationBuilder.DropColumn(
                name: "LikeCount",
                table: "DiscussionReplies");
        }
    }
}
