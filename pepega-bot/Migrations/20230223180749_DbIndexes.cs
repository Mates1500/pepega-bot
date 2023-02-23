using Microsoft.EntityFrameworkCore.Migrations;

namespace pepega_bot.Migrations
{
    public partial class DbIndexes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_WordEntries_Value",
                table: "WordEntries",
                column: "Value");

            migrationBuilder.CreateIndex(
                name: "IX_RingFitReacts_MessageTime",
                table: "RingFitReacts",
                column: "MessageTime");

            migrationBuilder.CreateIndex(
                name: "IX_RingFitReacts_UserId",
                table: "RingFitReacts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_EmoteStatMatches_TimestampUtc",
                table: "EmoteStatMatches",
                column: "TimestampUtc");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WordEntries_Value",
                table: "WordEntries");

            migrationBuilder.DropIndex(
                name: "IX_RingFitReacts_MessageTime",
                table: "RingFitReacts");

            migrationBuilder.DropIndex(
                name: "IX_RingFitReacts_UserId",
                table: "RingFitReacts");

            migrationBuilder.DropIndex(
                name: "IX_EmoteStatMatches_TimestampUtc",
                table: "EmoteStatMatches");
        }
    }
}
