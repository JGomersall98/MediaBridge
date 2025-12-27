using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MediaBridge.Migrations
{
    /// <inheritdoc />
    public partial class AddSonarrSeriesEndpoint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Only insert if the key doesn't already exist
            migrationBuilder.Sql(@"
                INSERT IGNORE INTO Configs (`Key`, `Value`, Description, Created)
                VALUES (
                    'sonarr_series_data_endpoint',
                    'http://192.168.3.120:8989/api/v3/series/{seriesId}?apiKey={ApiKey}',
                    'Sonarr API endpoint to get series data by series ID',
                    UTC_TIMESTAMP()
                )
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE FROM Configs 
                WHERE `Key` = 'sonarr_series_data_endpoint'
            ");
        }
    }
}
