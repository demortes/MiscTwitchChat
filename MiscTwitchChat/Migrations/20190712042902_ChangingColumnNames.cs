using Microsoft.EntityFrameworkCore.Migrations;

namespace MiscTwitchChat.Migrations
{
    public partial class ChangingColumnNames : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "name",
                table: "Disconsenters",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Disconsenters",
                newName: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Disconsenters",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Disconsenters",
                newName: "id");
        }
    }
}
