using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Post.Infrastructure.Migrations
{
    public partial class AddCommentLikeFeature : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add LikesCount to Comments
            migrationBuilder.AddColumn<int>(
                name: "LikesCount",
                schema: "Post",
                table: "Comments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Create CommentLikes table
            migrationBuilder.CreateTable(
                name: "CommentLikes",
                schema: "Post",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CommentId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommentLikes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommentLikes_Comments_CommentId",
                        column: x => x.CommentId,
                        principalSchema: "Post",
                        principalTable: "Comments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CommentLikes_UserId",
                schema: "Post",
                table: "CommentLikes",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CommentLikes_CommentId_UserId",
                schema: "Post",
                table: "CommentLikes",
                columns: new[] { "CommentId", "UserId" },
                unique: true,
                filter: "\"IsDeleted\" = false");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CommentLikes",
                schema: "Post");

            migrationBuilder.DropColumn(
                name: "LikesCount",
                schema: "Post",
                table: "Comments");
        }
    }
}
