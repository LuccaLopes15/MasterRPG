using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace T20Master.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddGameSessionEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GameSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Mode = table.Column<int>(type: "integer", nullable: false),
                    GridWidth = table.Column<int>(type: "integer", nullable: false),
                    GridHeight = table.Column<int>(type: "integer", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GameSessions_Campaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "Campaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CombatModifiers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterSheetId = table.Column<Guid>(type: "uuid", nullable: false),
                    Target = table.Column<int>(type: "integer", nullable: false),
                    Value = table.Column<int>(type: "integer", nullable: false),
                    Source = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CombatModifiers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CombatModifiers_CharacterSheets_CharacterSheetId",
                        column: x => x.CharacterSheetId,
                        principalTable: "CharacterSheets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CombatModifiers_GameSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "GameSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MapTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    GridX = table.Column<int>(type: "integer", nullable: false),
                    GridY = table.Column<int>(type: "integer", nullable: false),
                    CharacterSheetId = table.Column<Guid>(type: "uuid", nullable: true),
                    Label = table.Column<string>(type: "text", nullable: true),
                    MaxHP = table.Column<int>(type: "integer", nullable: true),
                    CurrentHP = table.Column<int>(type: "integer", nullable: true),
                    Color = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MapTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MapTokens_CharacterSheets_CharacterSheetId",
                        column: x => x.CharacterSheetId,
                        principalTable: "CharacterSheets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_MapTokens_GameSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "GameSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CombatModifiers_CharacterSheetId",
                table: "CombatModifiers",
                column: "CharacterSheetId");

            migrationBuilder.CreateIndex(
                name: "IX_CombatModifiers_SessionId",
                table: "CombatModifiers",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_GameSessions_CampaignId_Status",
                table: "GameSessions",
                columns: new[] { "CampaignId", "Status" },
                unique: true,
                filter: "\"Status\" = 0");

            migrationBuilder.CreateIndex(
                name: "IX_MapTokens_CharacterSheetId",
                table: "MapTokens",
                column: "CharacterSheetId");

            migrationBuilder.CreateIndex(
                name: "IX_MapTokens_SessionId",
                table: "MapTokens",
                column: "SessionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CombatModifiers");

            migrationBuilder.DropTable(
                name: "MapTokens");

            migrationBuilder.DropTable(
                name: "GameSessions");
        }
    }
}
