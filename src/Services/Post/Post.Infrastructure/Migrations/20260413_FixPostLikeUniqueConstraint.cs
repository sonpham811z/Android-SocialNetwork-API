using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Post.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixPostLikeUniqueConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Xóa constraint cũ
            migrationBuilder.DropIndex(
                name: "IX_PostLikes_PostId_UserId",
                schema: "Post",
                table: "PostLikes");

            // Tạo partial unique index chỉ cho các like chưa xoá
            // Cách này cho phép re-like sau khi unlike (soft delete)
            migrationBuilder.Sql(
                @"CREATE UNIQUE INDEX ""IX_PostLikes_PostId_UserId"" ON ""Post"".""PostLikes"" (""PostId"", ""UserId"") 
                  WHERE ""IsDeleted"" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Xóa partial index
            migrationBuilder.DropIndex(
                name: "IX_PostLikes_PostId_UserId",
                schema: "Post",
                table: "PostLikes");

            // Tạo lại unique index cũ
            migrationBuilder.CreateIndex(
                name: "IX_PostLikes_PostId_UserId",
                schema: "Post",
                table: "PostLikes",
                columns: new[] { "PostId", "UserId" },
                unique: true);
        }
    }
}
