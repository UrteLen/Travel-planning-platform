using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using TravelPlanner.Data;
using TravelPlanner.Exceptions;
using TravelPlanner.Service;
using TravelPlanner.Models;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;


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

builder.Services.AddExceptionHandler<VersionNotFoundExceptionHandler>();

builder.Services.AddDbContext<TravelPlannerDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));
builder.Services.AddScoped<TripService>();
builder.Services.AddScoped<ActivityService>();
builder.Services.AddScoped<ActivityManagementService>();
builder.Services.AddScoped<ActivitySchedulingService>();
builder.Services.AddScoped<ActivityVotingService>();
builder.Services.AddScoped<BudgetService>();
builder.Services.AddScoped<SettlementService>();
builder.Services.AddScoped<SummaryService>();
builder.Services.AddScoped<ActivityImportService>();
builder.Services.AddScoped<PdfExportService>();
builder.Services.AddScoped<TripVersionService>();
builder.Services.AddSingleton<VisitScheduler>();
builder.Services.AddScoped<DayPlanningService>();
builder.Services.AddScoped<PlanService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddExceptionHandler<AuthExceptionHandler>();
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey) || Encoding.UTF8.GetByteCount(jwtKey) < 32)
{
    throw new InvalidOperationException(
        "Jwt:Key is missing or shorter than 32 bytes. Locally set it with: " +
        "dotnet user-secrets set \"Jwt:Key\" \"<at least 32 characters>\" --project TravelPlanner. " +
        "On a deployed backend set the environment variable Jwt__Key.");
}
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "TravelPlanner",
            ValidateAudience = true,
            ValidAudience = "TravelPlanner",
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey))
        };
    });
var app = builder.Build();

app.UseCors("AllowAll");

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;

        if (app.Environment.IsDevelopment())
        {
            var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;

            await context.Response.WriteAsJsonAsync(new
            {
                error = "An unexpected server error occurred.",
                detail = exception?.ToString()
            });
        }
        else
        {
            await context.Response.WriteAsJsonAsync(new
            {
                error = "An unexpected server error occurred."
            });
        }
    });
});

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}
// app.UseHttpsRedirection();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapControllers();

app.Run();