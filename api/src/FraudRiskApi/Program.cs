using FraudRiskApi.Data;
using FraudRiskApi.Middleware;
using FraudRiskApi.Repositories;
using FraudRiskApi.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy
            .WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is missing.");

builder.Services.AddDbContext<FraudRiskDbContext>(options =>
{
    options.UseSqlServer(connectionString);
});

builder.Services.AddScoped<ITransactionRepository, SqlTransactionRepository>();
builder.Services.AddScoped<IAlertRepository, SqlAlertRepository>();
builder.Services.AddScoped<IFraudRiskModelEngine, PredictiveFraudModelEngine>();
builder.Services.AddScoped<IAiPredictionService, AiPredictionService>();
builder.Services.AddScoped<IFraudScoringService, FraudScoringService>();
builder.Services.AddScoped<IAlertService, AlertService>();
builder.Services.AddScoped<IReportingService, ReportingService>();

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<FraudRiskDbContext>();
    dbContext.Database.Migrate();
}

app.UseHttpsRedirection();

app.UseRouting();
app.UseCors("AllowAngular");
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program;
