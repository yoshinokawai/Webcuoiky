using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebWikiForum.Migrations
{
    /// <inheritdoc />
    public partial class AddEditorAssignmentAndDiscussionLock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsLocked",
                table: "Discussions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "EditorAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EditorUserId = table.Column<int>(type: "int", nullable: false),
                    DiscussionId = table.Column<int>(type: "int", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AssignedByAdminId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EditorAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EditorAssignments_Discussions_DiscussionId",
                        column: x => x.DiscussionId,
                        principalTable: "Discussions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EditorAssignments_Users_EditorUserId",
                        column: x => x.EditorUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EditorAssignments_DiscussionId",
                table: "EditorAssignments",
                column: "DiscussionId");

            migrationBuilder.CreateIndex(
                name: "IX_EditorAssignments_EditorUserId",
                table: "EditorAssignments",
                column: "EditorUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EditorAssignments");

            migrationBuilder.DropColumn(
                name: "IsLocked",
                table: "Discussions");
        }
    }
}
