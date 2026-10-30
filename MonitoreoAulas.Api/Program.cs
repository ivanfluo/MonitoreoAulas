using Microsoft.EntityFrameworkCore;
using MonitoreoAulas.Api.Data;
using MonitoreoAulas.Api.Hubs;
using MonitoreoAulas.Api.Services;
using MonitoreoAulas.Api.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Entity Framework Core + SQL Server
builder.Services.AddDbContext<MonitoreoDbContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("MonitoreoAulas"));
});

// SignalR: solamente ocupacion actual
builder.Services.AddSignalR();
builder.Services.AddScoped<IEventoOcupacionService, EventoOcupacionService>();
builder.Services.AddScoped<IOcupacionService, OcupacionService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Monitoreo de Aulas API v1");
        options.RoutePrefix = "swagger";
    });
}

app.UseAuthorization();
app.UseStaticFiles();
app.MapControllers();

app.MapHub<OcupacionHub>("/hubs/ocupacion");

app.MapGet("/health", () =>
{
    return Results.Ok(new
    {
        estado = "OK",
        sistema = "Monitoreo de Ocupacion de Aulas",
        fechaHoraUtc = DateTime.UtcNow
    });
});

app.Run();
