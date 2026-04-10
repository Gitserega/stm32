using System.Text;
using System.Text.Json;
using Diploma.Api.Hubs;
using Diploma.Api.Models;
using Diploma.Entity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MQTTnet;

namespace Diploma.Api.Services;

public class MqttListenerService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<VibrationHub> _hub;
    private readonly IConfiguration _config;
    private readonly ILogger<MqttListenerService> _logger;

    // DeviceId устройства STM32 — берётся из конфига или фиксированный
    private long _deviceId;

    public MqttListenerService(
        IServiceScopeFactory scopeFactory,
        IHubContext<VibrationHub> hub,
        IConfiguration config,
        ILogger<MqttListenerService> logger)
    {
        _scopeFactory = scopeFactory;
        _hub = hub;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Получаем или создаём устройство при старте
        await EnsureDeviceAsync(stoppingToken);

        var factory = new MqttClientFactory();
        using var client = factory.CreateMqttClient();

        client.ApplicationMessageReceivedAsync += OnMessageAsync;

        var options = new MqttClientOptionsBuilder()
            .WithTcpServer(
                _config["Mqtt:Host"] ?? "localhost",
                int.Parse(_config["Mqtt:Port"] ?? "1883"))
            .WithClientId("DipApi_Listener")
            .WithCleanSession()
            .Build();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await client.ConnectAsync(options, stoppingToken);
                _logger.LogInformation("MQTT connected to {Host}", _config["Mqtt:Host"]);

                var topicFilter = new MqttTopicFilterBuilder()
                    .WithTopic("stm32/vibration")
                    .Build();

                await client.SubscribeAsync(topicFilter, stoppingToken);
                _logger.LogInformation("Subscribed to stm32/vibration");

                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogWarning("MQTT error: {Msg}. Retry in 5s...", ex.Message);
                await Task.Delay(5000, stoppingToken);
            }
        }

        if (client.IsConnected)
            await client.DisconnectAsync();
    }

    /// <summary>
    /// Находит первое активное устройство в БД или создаёт дефолтное.
    /// DeviceId сохраняется в поле _deviceId для использования при записи измерений.
    /// </summary>
    private async Task EnsureDeviceAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var device = await db.Devices.FirstOrDefaultAsync(d => d.IsActive, ct);
        if (device is null)
        {
            device = new Device
            {
                Name = "STM32-Default",
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };
            db.Devices.Add(device);
            await db.SaveChangesAsync(ct);

            // Создаём дефолтные пороги для нового устройства
            db.Thresholds.AddRange(
                new ThresholdConfig { DeviceId = device.Id, Metric = "crest", Value = 4.0, UpdatedAt = DateTime.UtcNow },
                new ThresholdConfig { DeviceId = device.Id, Metric = "bearing", Value = 0.05, UpdatedAt = DateTime.UtcNow },
                new ThresholdConfig { DeviceId = device.Id, Metric = "gear", Value = 0.05, UpdatedAt = DateTime.UtcNow }
            );
            await db.SaveChangesAsync(ct);

            _logger.LogInformation("Created default device with Id={Id}", device.Id);
        }

        _deviceId = device.Id;
        _logger.LogInformation("Using DeviceId={Id}", _deviceId);
    }

    private async Task OnMessageAsync(MqttApplicationMessageReceivedEventArgs e)
    {
        var json = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
        _logger.LogInformation("MQTT payload: {Json}", json);

        MqttPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<MqttPayload>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Parse failed: {Err}", ex.Message);
            return;
        }

        if (payload is null) { _logger.LogWarning("Payload is null"); return; }
        if (payload.bl == 0) { _logger.LogInformation("bl=0, skipping"); return; }

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var alertService = scope.ServiceProvider.GetRequiredService<AlertService>();

        var measurement = new Measurement
        {
            DeviceId = _deviceId,          // ← ИСПРАВЛЕНО: обязательное поле
            ReceivedAt = DateTime.UtcNow,
            PacketNumber = payload.n,
            DeviceTimestamp = payload.ts,
            BaselineReady = payload.bl == 1,

            Z_Rms = payload.z.rms,
            Z_Crest = payload.z.crest,
            Z_Bear = payload.z.bear,
            Z_Gear = payload.z.gear,
            Z_Freq = payload.z.f,

            X_Rms = payload.x.rms,
            X_Crest = payload.x.crest,
            X_Bear = payload.x.bear,
            X_Gear = payload.x.gear,
            X_Freq = payload.x.f,

            Y_Rms = payload.y.rms,
            Y_Crest = payload.y.crest,
            Y_Bear = payload.y.bear,
            Y_Gear = payload.y.gear,
            Y_Freq = payload.y.f,
        };

        db.Measurements.Add(measurement);
        await db.SaveChangesAsync();

        var alerts = await alertService.CheckAndSaveAsync(measurement, payload);

        var dto = new VibrationDto
        {
            MeasurementId = measurement.Id,
            ReceivedAt = measurement.ReceivedAt,
            PacketNumber = measurement.PacketNumber,
            BaselineReady = measurement.BaselineReady,
            Z = new AxisDto { Rms = payload.z.rms, Crest = payload.z.crest, Bear = payload.z.bear, Gear = payload.z.gear, Freq = payload.z.f },
            X = new AxisDto { Rms = payload.x.rms, Crest = payload.x.crest, Bear = payload.x.bear, Gear = payload.x.gear, Freq = payload.x.f },
            Y = new AxisDto { Rms = payload.y.rms, Crest = payload.y.crest, Bear = payload.y.bear, Gear = payload.y.gear, Freq = payload.y.f },
            Alerts = alerts.Select(a => new AlertDto
            {
                Axis = a.Axis.ToString(),
                Metric = a.Metric.ToString(),
                Severity = a.Severity.ToString(),
                Value = a.Value,
                Threshold = a.Threshold
            }).ToList()
        };

        await _hub.Clients.All.SendAsync("ReceiveMeasurement", dto);

        _logger.LogInformation(
            "Published #{N} | Z rms={Zr:F4} crest={Zc:F2} | alerts={Count}",
            payload.n, payload.z.rms, payload.z.crest, alerts.Count);
    }
}