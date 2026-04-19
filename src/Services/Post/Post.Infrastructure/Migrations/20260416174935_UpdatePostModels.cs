using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Post.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePostModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PostLikes_PostId_UserId",
                schema: "Post",
                table: "PostLikes");

            migrationBuilder.CreateIndex(
                name: "IX_PostLikes_PostId_UserId",
                schema: "Post",
                table: "PostLikes",
                columns: new[] { "PostId", "UserId" },
                unique: true,
                filter: "\"IsDeleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PostLikes_PostId_UserId",
                schema: "Post",
                table: "PostLikes");

            migrationBuilder.CreateIndex(
                name: "IX_PostLikes_PostId_UserId",
                schema: "Post",
                table: "PostLikes",
                columns: new[] { "PostId", "UserId" },
                unique: true);
        }
    }
}
