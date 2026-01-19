using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MediaBridge.Migrations
{
    /// <inheritdoc />
    public partial class ChangeEpisodeDateToDateTime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // First, clear any existing invalid episode date values (the Ticks values)
            migrationBuilder.Sql("UPDATE download_requests SET EpisodeDate = NULL WHERE EpisodeDate IS NOT NULL;");

            // Then change the column type from int to datetime
            migrationBuilder.AlterColumn<DateTime>(
                name: "EpisodeDate",
                table: "download_requests",
                type: "datetime(6)",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "EpisodeDate",
                table: "download_requests",
                type: "int",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime(6)",
                oldNullable: true);
        }
    }
}
