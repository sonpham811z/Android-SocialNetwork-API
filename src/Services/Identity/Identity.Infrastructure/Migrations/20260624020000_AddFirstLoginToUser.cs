using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Identity.Infrastructure.Migrations
{
    public partial class AddFirstLoginToUser : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "FirstLogin",
                schema: "Identity",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            // Các tài khoản đã tồn tại (đã hoạt động) → không hiện lại phần giới thiệu.
            migrationBuilder.Sql("UPDATE \"Identity\".\"Users\" SET \"FirstLogin\" = false;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FirstLogin",
                schema: "Identity",
                table: "Users");
        }
    }
}
