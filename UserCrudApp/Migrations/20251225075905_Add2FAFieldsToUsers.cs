using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UserCrudApp.Migrations
{
    /// <inheritdoc />
    public partial class Add2FAFieldsToUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "dbo");

            migrationBuilder.CreateTable(
                name: "tbl_Users",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    createuid = table.Column<int>(type: "int", nullable: true),
                    createdt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    lmodifyby = table.Column<int>(type: "int", nullable: true),
                    lmodifydt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deluid = table.Column<int>(type: "int", nullable: true),
                    deldt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_Users", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tbl_Users",
                schema: "dbo");
        }
    }
}
