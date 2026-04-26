using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace T20Master.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddCharacterCoins : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CopperCoins",
                table: "CharacterSheets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "GoldCoins",
                table: "CharacterSheets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SilverCoins",
                table: "CharacterSheets",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CopperCoins",
                table: "CharacterSheets");

            migrationBuilder.DropColumn(
                name: "GoldCoins",
                table: "CharacterSheets");

            migrationBuilder.DropColumn(
                name: "SilverCoins",
                table: "CharacterSheets");
        }
    }
}
