using Npgsql;
using StockFlow.Core.Interfaces;
using StockFlow.Data;
using StockFlow.Data.Repositories;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("StockFlow")
    ?? throw new InvalidOperationException("Connection string 'StockFlow' is missing. Set it with user-secrets.");

builder.Services.AddSingleton(NpgsqlDataSource.Create(connectionString));
builder.Services.AddSingleton<IDbConnectionFactory, NpgsqlConnectionFactory>();

// Lets Dapper map unit_price -> UnitPrice automatically
Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IWarehouseRepository, WarehouseRepository>();

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();