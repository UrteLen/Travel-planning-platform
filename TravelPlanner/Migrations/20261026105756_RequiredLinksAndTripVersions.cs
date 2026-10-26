using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelPlanner.Migrations
{
    /// <inheritdoc />
    public partial class RequiredLinksAndTripVersions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExpenseEntry_Budget_BudgetId",
                table: "ExpenseEntry");

            migrationBuilder.DropForeignKey(
                name: "FK_Participant_Trips_TripId",
                table: "Participant");

            migrationBuilder.DropForeignKey(
                name: "FK_PlannedVisit_Trips_TripId",
                table: "PlannedVisit");

            migrationBuilder.DropForeignKey(
                name: "FK_ScheduledActivity_Trips_TripId",
                table: "ScheduledActivity");

            migrationBuilder.DropForeignKey(
                name: "FK_TripActivity_Trips_TripId",
                table: "TripActivity");

            migrationBuilder.AlterColumn<Guid>(
                name: "TripId",
                table: "TripActivity",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "TripId",
                table: "ScheduledActivity",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "TripId",
                table: "PlannedVisit",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "TripId",
                table: "Participant",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "BudgetId",
                table: "ExpenseEntry",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "TripVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TripId = table.Column<Guid>(type: "uuid", nullable: false),
                    VersionNumber = table.Column<int>(type: "integer", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ChangedBy = table.Column<string>(type: "text", nullable: false),
                    RestoredFromVersion = table.Column<int>(type: "integer", nullable: true),
                    BudgetJson = table.Column<string>(type: "jsonb", nullable: false),
                    PlanJson = table.Column<string>(type: "jsonb", nullable: false),
                    ActivitiesJson = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TripVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TripVersions_Trips_TripId",
                        column: x => x.TripId,
                        principalTable: "Trips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TripVersions_TripId",
                table: "TripVersions",
                column: "TripId");

            migrationBuilder.AddForeignKey(
                name: "FK_ExpenseEntry_Budget_BudgetId",
                table: "ExpenseEntry",
                column: "BudgetId",
                principalTable: "Budget",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Participant_Trips_TripId",
                table: "Participant",
                column: "TripId",
                principalTable: "Trips",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PlannedVisit_Trips_TripId",
                table: "PlannedVisit",
                column: "TripId",
                principalTable: "Trips",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ScheduledActivity_Trips_TripId",
                table: "ScheduledActivity",
                column: "TripId",
                principalTable: "Trips",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TripActivity_Trips_TripId",
                table: "TripActivity",
                column: "TripId",
                principalTable: "Trips",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExpenseEntry_Budget_BudgetId",
                table: "ExpenseEntry");

            migrationBuilder.DropForeignKey(
                name: "FK_Participant_Trips_TripId",
                table: "Participant");

            migrationBuilder.DropForeignKey(
                name: "FK_PlannedVisit_Trips_TripId",
                table: "PlannedVisit");

            migrationBuilder.DropForeignKey(
                name: "FK_ScheduledActivity_Trips_TripId",
                table: "ScheduledActivity");

            migrationBuilder.DropForeignKey(
                name: "FK_TripActivity_Trips_TripId",
                table: "TripActivity");

            migrationBuilder.DropTable(
                name: "TripVersions");

            migrationBuilder.AlterColumn<Guid>(
                name: "TripId",
                table: "TripActivity",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "TripId",
                table: "ScheduledActivity",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "TripId",
                table: "PlannedVisit",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "TripId",
                table: "Participant",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "BudgetId",
                table: "ExpenseEntry",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddForeignKey(
                name: "FK_ExpenseEntry_Budget_BudgetId",
                table: "ExpenseEntry",
                column: "BudgetId",
                principalTable: "Budget",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Participant_Trips_TripId",
                table: "Participant",
                column: "TripId",
                principalTable: "Trips",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PlannedVisit_Trips_TripId",
                table: "PlannedVisit",
                column: "TripId",
                principalTable: "Trips",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ScheduledActivity_Trips_TripId",
                table: "ScheduledActivity",
                column: "TripId",
                principalTable: "Trips",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TripActivity_Trips_TripId",
                table: "TripActivity",
                column: "TripId",
                principalTable: "Trips",
                principalColumn: "Id");
        }
    }
}
