using Microsoft.EntityFrameworkCore.Migrations;
using Pgvector;

#nullable disable

namespace T20Master.Web.Migrations
{
    /// <inheritdoc />
    public partial class UpdateEmbeddingTo3072 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RuleChunks_Embedding",
                table: "RuleChunks");

            migrationBuilder.AlterColumn<Vector>(
                name: "Embedding",
                table: "RuleChunks",
                type: "vector(3072)",
                nullable: false,
                oldClrType: typeof(Vector),
                oldType: "vector(768)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Vector>(
                name: "Embedding",
                table: "RuleChunks",
                type: "vector(768)",
                nullable: false,
                oldClrType: typeof(Vector),
                oldType: "vector(3072)");

            migrationBuilder.CreateIndex(
                name: "IX_RuleChunks_Embedding",
                table: "RuleChunks",
                column: "Embedding")
                .Annotation("Npgsql:IndexMethod", "hnsw")
                .Annotation("Npgsql:IndexOperators", new[] { "vector_cosine_ops" });
        }
    }
}
