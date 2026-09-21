using System.Text.Json;
using LedgerCore.Api.Middleware;
using LedgerCore.Application;
using LedgerCore.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
    });

var connectionString = builder.Configuration.GetConnectionString("LedgerCore")
    ?? "Host=localhost;Database=ledgercore;Username=postgres;Password=postgres";

builder.Services.AddLedgerCore(options => options.UsePostgreSql(connectionString));

builder.Services.AddScoped<PostEntryUseCase>();
builder.Services.AddScoped<VoidEntryUseCase>();
builder.Services.AddScoped<PostPendingEntryUseCase>();
builder.Services.AddScoped<GetBalanceUseCase>();
builder.Services.AddScoped<GetStatementUseCase>();
builder.Services.AddScoped<GetTrialBalanceUseCase>();
builder.Services.AddScoped<ClosePeriodUseCase>();
builder.Services.AddScoped<ReopenPeriodUseCase>();

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.MapControllers();

app.Run();
