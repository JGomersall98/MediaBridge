using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MediaBridge.Migrations
{
    /// <inheritdoc />
    public partial class downloaded_shows_release_date : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReleaseYear",
                table: "downloaded_shows");

            migrationBuilder.AddColumn<DateTime>(
                name: "ReleaseDate",
                table: "downloaded_shows",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReleaseDate",
                table: "downloaded_shows");

            migrationBuilder.AddColumn<int>(
                name: "ReleaseYear",
                table: "downloaded_shows",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
