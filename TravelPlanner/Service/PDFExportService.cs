using System;
using System.IO;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TravelPlanner.Reports;

namespace TravelPlanner.Service
{
    public class PdfExportService
    {
        public byte[] ExportToPdf(TripSummary summary)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);

                    page.Header().Text("Trip Summary").FontSize(20).Bold();

                    page.Content().Column(column =>
                    {
                        column.Item().Text($"Generated: {summary.GeneratedAt:g}");
                        column.Item().Text($"Total planned budget: {summary.TotalPlannedBudget:C}");
                        column.Item().Text($"Total actual spend: {summary.TotalActualSpend:C}");
                        column.Item().Text($"Cost per person: {summary.CostPerPerson:C}");

                        column.Item().PaddingTop(10).Text("Budget by category").Bold();
                        foreach (var category in summary.CategoryBreakdown)
                        {
                            column.Item().Text($"{category.Category}: {category.ActualSpent:C} / {category.Limit:C} — {category.Status}");
                        }

                        column.Item().PaddingTop(10).Text("Itinerary").Bold();
                        foreach (var visit in summary.Itinerary)
                        {
                            column.Item().Text($"{visit.ArrivalTime:t} - {visit.DepartureTime:t}: {visit.Place.Name}");
                        }
                    });
                });
            });

            return document.GeneratePdf();
        }
    }
}