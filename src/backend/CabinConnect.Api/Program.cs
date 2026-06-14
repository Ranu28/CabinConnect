using CabinConnect.Domain.Holds;
using CabinConnect.Domain.Search;
using CabinConnect.Infrastructure.Common;
using CabinConnect.Infrastructure.Holds;
using CabinConnect.Infrastructure.Search;
using Dapper;
using DotNetEnv;
using Npgsql;

// Load .env.local (gitignored) before the host reads configuration.
// Variables use double-underscore for nesting: ConnectionStrings__Supabase=...
// No-op if the file does not exist (e.g. in CI where env vars are injected directly).
Env.Load(".env.local");

// Register Dapper type handler for DateOnly ↔ PostgreSQL date once at startup.
SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());

var builder = WebApplication.CreateBuilder(args);

// NpgsqlDataSource is the recommended way to create Npgsql connections.
// Connection string is read from configuration (appsettings / env vars / user-secrets).
// Never hardcode the connection string here — use environment variables in production.
// Defer validation to resolution time so WebApplicationFactory can boot without
// a real connection string (the repository is mocked in integration tests).
var connectionString = builder.Configuration.GetConnectionString("Supabase");
builder.Services.AddSingleton(_ =>
    connectionString is null
        ? throw new InvalidOperationException(
            "Connection string 'Supabase' is missing. " +
            "Add it to appsettings.Development.json or as an environment variable " +
            "ConnectionStrings__Supabase.")
        : NpgsqlDataSource.Create(connectionString));

// Auth services — scheme is configured by the auth unit (ADR-005).
// AddAuthentication() registers the middleware services so UseAuthentication() works.
builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddScoped<ICabinSearchRepository, CabinSearchRepository>();
builder.Services.AddScoped<CabinSearchService>();

builder.Services.AddScoped<IHoldRepository, HoldRepository>();
builder.Services.AddScoped<HoldService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program { } // for WebApplicationFactory in integration tests
