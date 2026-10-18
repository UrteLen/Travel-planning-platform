using TravelPlanner.Service;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddControllersWithViews();
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(
        new System.Text.Json.Serialization.JsonStringEnumConverter()));

builder.Services.AddSingleton<TripService>();
builder.Services.AddScoped<ActivityService>();
builder.Services.AddScoped<ActivitySchedulingService>();
builder.Services.AddScoped<ActivityVotingService>();
builder.Services.AddScoped<BudgetService>();
builder.Services.AddScoped<SettlementService>();
builder.Services.AddScoped<SummaryService>();
builder.Services.AddScoped<ActivityImportService>();
builder.Services.AddScoped<PdfExportService>();
builder.Services.AddSingleton<TripVersionService>();
builder.Services.AddSingleton<VisitScheduler>();
builder.Services.AddScoped<DayPlanningService>();
builder.Services.AddScoped<PlanService>();

var app = builder.Build();

app.UseCors("AllowAll");

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler(errorApp =>
    {
        errorApp.Run(async context =>
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;

            await context.Response.WriteAsJsonAsync(new
            {
                error = "An unexpected server error occurred."
            });
        });
    });
    app.UseHsts();
}
// app.UseHttpsRedirection();

app.UseRouting();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapControllers();

app.Run();