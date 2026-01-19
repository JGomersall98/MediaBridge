using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MediaBridge.Migrations
{
    /// <inheritdoc />
    public partial class addupdatedat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "download_requests",
                type: "datetime(6)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "download_requests");
        }
    }
}
