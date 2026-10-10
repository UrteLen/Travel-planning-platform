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
builder.Services.AddControllers();
builder.Services.AddSingleton<TripService>();
builder.Services.AddScoped<ActivityService>();
builder.Services.AddScoped<BudgetService>();
builder.Services.AddScoped<SettlementService>();
builder.Services.AddScoped<SummaryService>();
builder.Services.AddScoped<PdfExportService>();
builder.Services.AddSingleton<PlanVersionService>();
builder.Services.AddSingleton<VisitScheduler>();
builder.Services.AddScoped<DayPlanningService>();

var app = builder.Build();

app.UseCors("AllowAll");

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
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