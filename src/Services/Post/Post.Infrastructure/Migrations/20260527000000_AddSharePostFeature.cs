using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Post.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSharePostFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "OriginalPostId",
                schema: "Post",
                table: "Posts",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Posts_OriginalPostId",
                schema: "Post",
                table: "Posts",
                column: "OriginalPostId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Posts_OriginalPostId",
                schema: "Post",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "OriginalPostId",
                schema: "Post",
                table: "Posts");
        }
    }
}
