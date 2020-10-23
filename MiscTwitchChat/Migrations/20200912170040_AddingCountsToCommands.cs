using Microsoft.EntityFrameworkCore.Migrations;

namespace MiscTwitchChat.Migrations
{
    public partial class AddingCountsToCommands : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CommandCounts",
                columns: table => new
                {
                    Channel = table.Column<string>(nullable: false),
                    TargetUser = table.Column<string>(nullable: false),
                    CommandUsed = table.Column<string>(nullable: false),
                    Count = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommandCounts", x => new { x.Channel, x.CommandUsed, x.TargetUser });
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CommandCounts");
        }
    }
}
