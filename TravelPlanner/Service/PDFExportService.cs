using System;
using System.IO;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TravelPlanner.Reports;
using TravelPlanner.Extensions;

namespace TravelPlanner.Service;

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
                    column.Item().Text($"Total planned budget: {summary.TotalPlannedBudget.ToCurrencyString()}");
                    column.Item().Text($"Total actual spend: {summary.TotalActualSpend.ToCurrencyString()}");
                    column.Item().Text($"Cost per person: {summary.CostPerPerson.ToCurrencyString()}");

                    column.Item().PaddingTop(10).Text("Budget by category").Bold();
                    foreach (var category in summary.CategoryBreakdown)
                    {
                        column.Item().Text($"{category.Category}: {category.ActualSpent.ToCurrencyString()} / {category.Limit.ToCurrencyString()} — {category.Status}");
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
