using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MediaBridge.Migrations
{
    /// <inheritdoc />
    public partial class downloaded_shows : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "downloaded_shows",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Type = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Title = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    HasFile = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    SeasonNumber = table.Column<int>(type: "int", nullable: true),
                    EpisodesInSeason = table.Column<int>(type: "int", nullable: true),
                    EpisodesDownloaded = table.Column<int>(type: "int", nullable: true),
                    DownloadedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ReleaseYear = table.Column<int>(type: "int", nullable: false),
                    ImdbId = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TvdbId = table.Column<int>(type: "int", nullable: false),
                    PosterPath = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SizeOnDiskGB = table.Column<double>(type: "double", nullable: true),
                    PhysicalPath = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Monitored = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Added = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_downloaded_shows", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "downloaded_shows");
        }
    }
}
