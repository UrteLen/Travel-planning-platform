using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelPlanner.Migrations
{
    /// <inheritdoc />
    public partial class AddParticipantUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "Participant",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Participant_UserId",
                table: "Participant",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Participant_Users_UserId",
                table: "Participant",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Participant_Users_UserId",
                table: "Participant");

            migrationBuilder.DropIndex(
                name: "IX_Participant_UserId",
                table: "Participant");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Participant");
        }
    }
}
