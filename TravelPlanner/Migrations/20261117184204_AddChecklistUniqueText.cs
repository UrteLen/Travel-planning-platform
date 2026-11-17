using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelPlanner.Migrations
{
    /// <inheritdoc />
    public partial class AddChecklistUniqueText : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE UNIQUE INDEX "UX_ChecklistItems_ParticipantId_Category_LowerText"
                ON "ChecklistItems" ("ParticipantId", "Category", lower("Text"));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""DROP INDEX "UX_ChecklistItems_ParticipantId_Category_LowerText";""");
        }
    }
}
