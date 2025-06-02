using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SignalRGame.Migrations
{
    /// <inheritdoc />
    public partial class Intercation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "interaction",
                table: "Users",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "platform",
                table: "Users",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "interaction",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "platform",
                table: "Users");
        }
    }
}
