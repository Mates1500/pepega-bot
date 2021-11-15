using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace pepega_bot.Migrations
{
    public partial class EmoteStatMatch : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmoteStatMatches",
                columns: table => new
                {
                    Id = table.Column<uint>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<ulong>(nullable: false),
                    MessageLength = table.Column<int>(nullable: false),
                    MatchesCount = table.Column<int>(nullable: false),
                    TimestampUtc = table.Column<DateTime>(nullable: false),
                    MessageId = table.Column<ulong>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmoteStatMatches", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmoteStatMatches");
        }
    }
}
