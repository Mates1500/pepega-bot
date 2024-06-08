using System;
using System.IO;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;

namespace pepega_bot.Migrations
{
    public partial class RingFitInteract : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsApproximateValue",
                table: "RingFitReacts",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<uint>(
                name: "MinuteValue",
                table: "RingFitReacts",
                nullable: false,
                defaultValue: 0);

            var config = new ConfigurationBuilder().SetBasePath(Path.Join(Directory.GetCurrentDirectory(), "VolData"))
                .AddJsonFile("config.json")
#if DEBUG
                .AddJsonFile("config.dev.json")
#endif
                .Build();

            // map existing values from emote strings in config to their numerical values and mark them as IsApproximateValue
            foreach (var pair in config.GetSection("RingFit:ScoreMapping").GetChildren())
            {
                var currentPair = pair.Get<string[]>();

                if (currentPair.Length != 2)
                    throw new FormatException("Score mapping per item is expected in form [\"EmoteKey\", MinuteValue]");

                var emoteKey = currentPair[0];
                var minuteValue = Convert.ToInt16(currentPair[1]);

                migrationBuilder.UpdateData(
                    table: "RingFitReacts",
                    keyColumn: "EmoteId",
                    keyValue: emoteKey,
                    columns: new[] { "IsApproximateValue", "MinuteValue" },
                    values: new object[] { true, minuteValue }
                );
            }
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsApproximateValue",
                table: "RingFitReacts");

            migrationBuilder.DropColumn(
                name: "MinuteValue",
                table: "RingFitReacts");
        }
    }
}
