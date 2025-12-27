using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MediaBridge.Migrations
{
    /// <inheritdoc />
    public partial class addepisodeid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EpisodeId",
                table: "download_requests",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EpisodeId",
                table: "download_requests");
        }
    }
}
