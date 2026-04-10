using BCrypt.Net;
using Diploma.Api.Hubs;
using Diploma.Api.Services;
using Diploma.DTO;
using Diploma.DTO.Auth;
using Diploma.DTO.Config;
using Diploma.DTO.History;
using Diploma.DTO.User;
using Diploma.Entity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

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

var jwtKey = builder.Configuration["Jwt:Key"] ?? "your-secret-key-at-least-32-characters-long";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "AttendanceAPI";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "AttendanceClient";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddScoped<AuthService>();
var app = builder.Build();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseCors("blazor");
app.MapHub<VibrationHub>("/hubs/vibration");
app.MapGet("/", () => "Hello World!");
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapPost("/api/auth/login", async (Diploma.DTO.Auth.LoginRequest request, AppDbContext db, AuthService authService) =>
{
    var user = await db.Users
        .Include(u => u.Role)
        .FirstOrDefaultAsync(u => u.Login == request.Login);

    if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
        return Results.Unauthorized();

    var token = authService.GenerateToken(user);
    return Results.Ok(new LoginResponse
    {
        Token = token,
        UserId = user.Id,
        Login = user.Login,
        Role = user.Role.Name
    });
});
app.MapGet("/api/history/data", async (DateTime start, DateTime end, string parameter, AppDbContext db) =>
{
    // start и end уже в UTC (т.к. передаём с Z)
    var startDate = start.Date;
    var endDate = end.Date.AddDays(1);

    var measurements = await db.Measurements
        .Where(m => m.ReceivedAt.Date >= startDate && m.ReceivedAt.Date < endDate)
        .OrderBy(m => m.ReceivedAt)
        .Select(m => new HistoryDataPoint
        {
            Timestamp = m.ReceivedAt,
            X = parameter == "Rms" ? m.X_Rms :
                parameter == "Crest" ? m.X_Crest :
                parameter == "Bear" ? m.X_Bear : m.X_Gear,
            Y = parameter == "Rms" ? m.Y_Rms :
                parameter == "Crest" ? m.Y_Crest :
                parameter == "Bear" ? m.Y_Bear : m.Y_Gear,
            Z = parameter == "Rms" ? m.Z_Rms :
                parameter == "Crest" ? m.Z_Crest :
                parameter == "Bear" ? m.Z_Bear : m.Z_Gear,
        })
        .ToListAsync();

    return Results.Ok(measurements);
}).RequireAuthorization(a => a.RequireRole("Operator"));
app.MapGet("/api/alerts", async (DateTime? from, DateTime? to, AppDbContext db) =>
{
    var query = db.Alerts.Include(a => a.Measurement).AsQueryable();

    if (from.HasValue)
    {
        var fromUtc = from.Value.ToUniversalTime();
        query = query.Where(a => a.TriggeredAt >= fromUtc);
    }
    if (to.HasValue)
    {
        var toUtc = to.Value.ToUniversalTime().AddDays(1);
        query = query.Where(a => a.TriggeredAt < toUtc);
    }

    var alerts = await query
        .OrderByDescending(a => a.TriggeredAt)
        .Select(a => new AlertDto   // ← явное создание DTO
        {
            Id = a.Id,
            TriggeredAt = a.TriggeredAt,
            Severity = a.Severity.ToString(),
            Axis = a.Axis.ToString(),
            Metric = a.Metric.ToString(),
            Value = a.Value,
            Threshold = a.Threshold,
            MeasurementId = a.MeasurementId
        })
        .ToListAsync();

    return Results.Ok(alerts);
}).RequireAuthorization(a => a.RequireRole("Operator")); 
// Получить всех пользователей (с их ролями)
app.MapGet("/api/admin/users", async (AppDbContext db) =>
{
    var users = await db.Users
        .Include(u => u.Role)
        .Select(u => new UserDto
        {
            Id = u.Id,
            Login = u.Login,
            Role = u.Role.Name // "Admin" или "Operator"
        })
        .ToListAsync();
    return Results.Ok(users);
}).RequireAuthorization(a => a.RequireRole("Admin"));

// Создать пользователя
app.MapPost("/api/admin/users", async (CreateUserRequest request, AppDbContext db) =>
{
    // Проверка уникальности логина
    var exists = await db.Users.AnyAsync(u => u.Login == request.Login);
    if (exists) return Results.Conflict("Login already exists");

    // Определяем RoleId: предположим, Admin имеет Id=1, Operator Id=2
    var roleId = request.IsAdmin ? 1L : 2L;

    var user = new User
    {
        Login = request.Login,
        Password = BCrypt.Net.BCrypt.HashPassword(request.Password),
        RoleId = roleId
    };
    db.Users.Add(user);
    await db.SaveChangesAsync();
    return Results.Ok();
}).RequireAuthorization(a => a.RequireRole("Admin"));

app.MapPut("/api/admin/users/{id}", async (long id, UpdateUserRequest request, AppDbContext db) =>
{
    var user = await db.Users.FindAsync(id);
    if (user == null) return Results.NotFound();

    // Логин
    if (!string.IsNullOrWhiteSpace(request.Login) && request.Login != user.Login)
    {
        var exists = await db.Users.AnyAsync(u => u.Login == request.Login && u.Id != id);
        if (exists) return Results.Conflict("Login already taken");
        user.Login = request.Login;
    }

    // Пароль
    if (!string.IsNullOrWhiteSpace(request.Password))
        user.Password = BCrypt.Net.BCrypt.HashPassword(request.Password);

    // Роль
    if (request.IsAdmin.HasValue)
    {
        var roleName = request.IsAdmin.Value ? "Admin" : "Operator";
        var role = await db.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
        if (role == null) return Results.BadRequest("Role not found");
        user.RoleId = role.Id;
    }

    await db.SaveChangesAsync();
    return Results.Ok();
}).RequireAuthorization(a => a.RequireRole("Admin"));

// Удалить пользователя
app.MapDelete("/api/admin/users/{id}", async (long id, AppDbContext db) =>
{
    var user = await db.Users.FindAsync(id);
    if (user == null) return Results.NotFound();
    db.Users.Remove(user);
    await db.SaveChangesAsync();
    return Results.Ok();
}).RequireAuthorization(a => a.RequireRole("Admin"));

// Получить пороги для всех устройств
app.MapGet("/api/admin/devices/thresholds", async (AppDbContext db) =>
{
    var devices = await db.Devices
        .Where(d => d.IsActive)
        .Select(d => new DeviceThresholdDto
        {
            DeviceId = d.Id,
            DeviceName = d.Name,
            Rms = d.ThresholdConfigs.FirstOrDefault(t => t.Metric == "rms").Value,
            Crest = d.ThresholdConfigs.FirstOrDefault(t => t.Metric == "crest").Value,
            Bearing = d.ThresholdConfigs.FirstOrDefault(t => t.Metric == "bearing").Value,
            Gear = d.ThresholdConfigs.FirstOrDefault(t => t.Metric == "gear").Value
        })
        .ToListAsync();

    return Results.Ok(devices);
}).RequireAuthorization(a => a.RequireRole("Admin"));

// Обновить пороги устройства
app.MapPut("/api/admin/devices/thresholds", async (UpdateThresholdsRequest request, AppDbContext db) =>
{
    var device = await db.Devices
        .Include(d => d.ThresholdConfigs)
        .FirstOrDefaultAsync(d => d.Id == request.DeviceId);

    if (device == null) return Results.NotFound("Device not found");

    async Task UpdateOrCreateThreshold(string metric, double? value)
    {
        if (!value.HasValue) return;

        var existing = device.ThresholdConfigs.FirstOrDefault(t => t.Metric == metric);
        if (existing != null)
        {
            existing.Value = value.Value;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            db.Thresholds.Add(new ThresholdConfig
            {
                DeviceId = device.Id,
                Metric = metric,
                Value = value.Value,
                UpdatedAt = DateTime.UtcNow
            });
        }
    }

    await UpdateOrCreateThreshold("rms", request.Rms);
    await UpdateOrCreateThreshold("crest", request.Crest);
    await UpdateOrCreateThreshold("bearing", request.Bearing);
    await UpdateOrCreateThreshold("gear", request.Gear);

    await db.SaveChangesAsync();
    return Results.Ok();
}).RequireAuthorization(a => a.RequireRole("Admin"));
// Создать новое устройство с опциональными порогами
app.MapPost("/api/admin/devices", async (CreateDeviceRequest request, AppDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(request.Name))
        return Results.BadRequest("Device name is required");

    var device = new Device
    {
        Name = request.Name,
        IsActive = true,
        CreatedAt = DateTime.UtcNow
    };
    db.Devices.Add(device);
    await db.SaveChangesAsync();

    // Добавляем пороги, если указаны
    async Task AddThresholdIfNotNull(string metric, double? value)
    {
        if (value.HasValue)
        {
            db.Thresholds.Add(new ThresholdConfig
            {
                DeviceId = device.Id,
                Metric = metric,
                Value = value.Value,
                UpdatedAt = DateTime.UtcNow
            });
        }
    }

    await AddThresholdIfNotNull("rms", request.Rms);
    await AddThresholdIfNotNull("crest", request.Crest);
    await AddThresholdIfNotNull("bearing", request.Bearing);
    await AddThresholdIfNotNull("gear", request.Gear);

    await db.SaveChangesAsync();
    return Results.Ok(new { device.Id });
}).RequireAuthorization(a => a.RequireRole("Admin"));
app.Run();


public class AuthService
{
    private readonly IConfiguration _configuration;

    public AuthService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateToken(User user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Login),
            new Claim(ClaimTypes.Role, user.Role?.Name ?? "")
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? "your-secret-key-at-least-32-characters-long"));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"] ?? "AttendanceAPI",
            audience: _configuration["Jwt:Audience"] ?? "AttendanceClient",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
