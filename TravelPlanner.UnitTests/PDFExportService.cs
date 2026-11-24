using System.Text;
using TravelPlanner.Enums;
using TravelPlanner.Models;
using TravelPlanner.Reports;
using TravelPlanner.Responses;
using TravelPlanner.Service;

namespace TravelPlanner.UnitTests;

public class PdfExportServiceTests
{
    private static Place CreatePlace(string name) => new()
    {
        Name = name,
        Location = new GeoLocation(41.9, 12.5),
        VisitDuration = TimeSpan.FromHours(1)
    };

    [Fact]
    public void ExportToPdf_ReturnsAPdfDocument()
    {
        var day = new DateTime(2026, 12, 1);
        var summary = new TripSummary(
            150m,
            30m,
            10m,
            new List<CategoryBudgetResult>
            {
                new(Category.Food, 100m, 30m, BudgetStatus.Ok)
            },
            new List<PlannedVisit>
            {
                new(CreatePlace("Pantheon"), day.AddHours(14), day.AddHours(15)),
                new(CreatePlace("Forum"), day.AddHours(9), day.AddHours(11))
            },
            day);

        var bytes = new PdfExportService().ExportToPdf(summary);

        Assert.True(bytes.Length > 0);
        Assert.Equal("%PDF", Encoding.ASCII.GetString(bytes, 0, 4));
    }

    [Fact]
    public void ExportToPdf_WithEmptySummary_StillReturnsAPdfDocument()
    {
        var summary = new TripSummary(
            0m,
            0m,
            0m,
            new List<CategoryBudgetResult>(),
            new List<PlannedVisit>(),
            DateTime.Now);

        var bytes = new PdfExportService().ExportToPdf(summary);

        Assert.Equal("%PDF", Encoding.ASCII.GetString(bytes, 0, 4));
    }
}
