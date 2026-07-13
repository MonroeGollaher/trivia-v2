using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TriviaGame.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddIsEndedToGame : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsEnded",
                table: "Games",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsEnded",
                table: "Games");
        }
    }
}
