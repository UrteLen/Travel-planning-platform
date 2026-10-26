using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelPlanner.Migrations
{
    /// <inheritdoc />
    public partial class LockedWritesAndLocalTimes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TripVersions_TripId",
                table: "TripVersions");

            // Times saved so far were stored as UTC; keep the same clock time, just drop the time zone.
            migrationBuilder.Sql(@"ALTER TABLE ""ScheduledActivity"" ALTER COLUMN ""StartTime"" TYPE timestamp without time zone USING ""StartTime"" AT TIME ZONE 'UTC';");
            migrationBuilder.Sql(@"ALTER TABLE ""PlannedVisit"" ALTER COLUMN ""ArrivalTime"" TYPE timestamp without time zone USING ""ArrivalTime"" AT TIME ZONE 'UTC';");
            migrationBuilder.Sql(@"ALTER TABLE ""PlannedVisit"" ALTER COLUMN ""DepartureTime"" TYPE timestamp without time zone USING ""DepartureTime"" AT TIME ZONE 'UTC';");

            // The old code could save two versions with the same number. Renumber 1, 2, 3... per trip
            // in the order they were saved, so the unique index below can be created.
            migrationBuilder.Sql(@"
                UPDATE ""TripVersions"" AS v
                SET ""VersionNumber"" = ordered.""NewNumber""
                FROM (
                    SELECT ""Id"", ROW_NUMBER() OVER (PARTITION BY ""TripId"" ORDER BY ""Timestamp"", ""VersionNumber"") AS ""NewNumber""
                    FROM ""TripVersions""
                ) AS ordered
                WHERE v.""Id"" = ordered.""Id"";");

            migrationBuilder.CreateIndex(
                name: "IX_TripVersions_TripId_VersionNumber",
                table: "TripVersions",
                columns: new[] { "TripId", "VersionNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TripVersions_TripId_VersionNumber",
                table: "TripVersions");

            migrationBuilder.AlterColumn<DateTime>(
                name: "StartTime",
                table: "ScheduledActivity",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DepartureTime",
                table: "PlannedVisit",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ArrivalTime",
                table: "PlannedVisit",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.CreateIndex(
                name: "IX_TripVersions_TripId",
                table: "TripVersions",
                column: "TripId");
        }
    }
}
