using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Post.Infrastructure.Migrations
{
    public partial class AddCampusBoardFeature : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BoardPosts",
                schema: "Post",
                columns: table => new
                {
                    Id          = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorId    = table.Column<Guid>(type: "uuid", nullable: true),
                    Tag         = table.Column<int>(type: "integer", nullable: false),
                    Content     = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    UpvotesCount   = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    DownvotesCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    CommentsCount  = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    IsAnonymous = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt   = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted   = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAt   = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BoardPosts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BoardVotes",
                schema: "Post",
                columns: table => new
                {
                    Id          = table.Column<Guid>(type: "uuid", nullable: false),
                    BoardPostId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId      = table.Column<Guid>(type: "uuid", nullable: false),
                    Type        = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt   = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted   = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAt   = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BoardVotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BoardVotes_BoardPosts_BoardPostId",
                        column: x => x.BoardPostId,
                        principalSchema: "Post",
                        principalTable: "BoardPosts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex("IX_BoardPosts_Tag",       schema: "Post", table: "BoardPosts", column: "Tag");
            migrationBuilder.CreateIndex("IX_BoardPosts_CreatedAt",  schema: "Post", table: "BoardPosts", column: "CreatedAt");
            migrationBuilder.CreateIndex("IX_BoardPosts_AuthorId",   schema: "Post", table: "BoardPosts", column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_BoardVotes_BoardPostId_UserId",
                schema: "Post",
                table: "BoardVotes",
                columns: new[] { "BoardPostId", "UserId" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.Sql(@"
                INSERT INTO ""Post"".""__EFMigrationsHistory"" (""MigrationId"", ""ProductVersion"")
                VALUES ('20260601000000_AddCampusBoardFeature', '9.0.0')
                ON CONFLICT DO NOTHING;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "BoardVotes", schema: "Post");
            migrationBuilder.DropTable(name: "BoardPosts", schema: "Post");
        }
    }
}
