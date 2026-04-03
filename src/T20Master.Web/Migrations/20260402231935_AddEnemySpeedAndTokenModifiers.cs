using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace T20Master.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddEnemySpeedAndTokenModifiers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Speed",
                table: "MapTokens",
                type: "numeric",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "CharacterSheetId",
                table: "CombatModifiers",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "MapTokenId",
                table: "CombatModifiers",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CombatModifiers_MapTokenId",
                table: "CombatModifiers",
                column: "MapTokenId");

            migrationBuilder.AddForeignKey(
                name: "FK_CombatModifiers_MapTokens_MapTokenId",
                table: "CombatModifiers",
                column: "MapTokenId",
                principalTable: "MapTokens",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CombatModifiers_MapTokens_MapTokenId",
                table: "CombatModifiers");

            migrationBuilder.DropIndex(
                name: "IX_CombatModifiers_MapTokenId",
                table: "CombatModifiers");

            migrationBuilder.DropColumn(
                name: "Speed",
                table: "MapTokens");

            migrationBuilder.DropColumn(
                name: "MapTokenId",
                table: "CombatModifiers");

            migrationBuilder.AlterColumn<Guid>(
                name: "CharacterSheetId",
                table: "CombatModifiers",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
