using Diploma.Api.Hubs;
using Diploma.Api.Services;
using Diploma.Entity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Default")));
builder.Services.AddScoped<AlertService>();
builder.Services.AddHostedService<MqttListenerService>();
builder.Services.AddSignalR();
builder.Services.AddCors(opt => opt.AddPolicy("blazor", policy =>
    policy
        .WithOrigins(
            builder.Configuration["Cors:BlazorOrigin"] ?? "https://localhost:7001")
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()));
var app = builder.Build();
app.UseHttpsRedirection();
app.UseCors("blazor");
app.MapHub<VibrationHub>("/hubs/vibration");
app.MapGet("/", () => "Hello World!");
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();



app.Run();

