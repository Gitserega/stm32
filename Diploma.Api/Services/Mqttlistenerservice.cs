using System.Text;
using System.Text.Json;
using Diploma.Api.Hubs;
using Diploma.Api.Models;
using Diploma.Entity;
using Microsoft.AspNetCore.SignalR;
using MQTTnet;

namespace Diploma.Api.Services;

public class MqttListenerService : BackgroundService
{
    private readonly IServiceScopeFactory      _scopeFactory;
    private readonly IHubContext<VibrationHub> _hub;
    private readonly IConfiguration            _config;
    private readonly ILogger<MqttListenerService> _logger;

    public MqttListenerService(
        IServiceScopeFactory scopeFactory,
        IHubContext<VibrationHub> hub,
        IConfiguration config,
        ILogger<MqttListenerService> logger)
    {
        _scopeFactory = scopeFactory;
        _hub          = hub;
        _config       = config;
        _logger       = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        /* MQTTnet v5: MqttClientFactory вместо MqttFactory */
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

                /* MQTTnet v5: подписка через MqttTopicFilter */
                var topicFilter = new MqttTopicFilterBuilder()
                    .WithTopic("stm32/vibration")
                    .Build();

                await client.SubscribeAsync(topicFilter, stoppingToken);
                _logger.LogInformation("Subscribed to stm32/vibration");

                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("MQTT error: {Msg}. Retry in 5s...", ex.Message);
                await Task.Delay(5000, stoppingToken);
            }
        }

        if (client.IsConnected)
            await client.DisconnectAsync();
    }

    private async Task OnMessageAsync(MqttApplicationMessageReceivedEventArgs e)
    {
        _logger.LogInformation("RAW message received from topic: {Topic}", e.ApplicationMessage.Topic);

        var json = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
        _logger.LogInformation("Payload: {Json}", json);

        MqttPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<MqttPayload>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            _logger.LogInformation("Parsed OK: bl={Bl}, n={N}", payload?.bl, payload?.n);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Parse FAILED: {Err}", ex.Message);
            return;
        }

        if (payload is null) { _logger.LogWarning("Payload is null"); return; }
        if (payload.bl == 0) { _logger.LogInformation("bl=0, skipping"); return; }

        _logger.LogInformation("Saving to DB...");

        using var scope      = _scopeFactory.CreateScope();
        var db               = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var alertService     = scope.ServiceProvider.GetRequiredService<AlertService>();

        var measurement = new Measurement
        {
            ReceivedAt      = DateTime.UtcNow,
            PacketNumber    = payload.n,
            DeviceTimestamp = payload.ts,
            BaselineReady   = payload.bl == 1,

            Z_Rms   = payload.z.rms,   Z_Crest = payload.z.crest,
            Z_Bear  = payload.z.bear,  Z_Gear  = payload.z.gear,  Z_Freq = payload.z.f,

            X_Rms   = payload.x.rms,   X_Crest = payload.x.crest,
            X_Bear  = payload.x.bear,  X_Gear  = payload.x.gear,  X_Freq = payload.x.f,

            Y_Rms   = payload.y.rms,   Y_Crest = payload.y.crest,
            Y_Bear  = payload.y.bear,  Y_Gear  = payload.y.gear,  Y_Freq = payload.y.f,
        };

        db.Measurements.Add(measurement);
        await db.SaveChangesAsync();

        var alerts = await alertService.CheckAndSaveAsync(measurement, payload);

        var dto = new VibrationDto
        {
            MeasurementId = measurement.Id,
            ReceivedAt    = measurement.ReceivedAt,
            PacketNumber  = measurement.PacketNumber,
            BaselineReady = measurement.BaselineReady,
            Z = new AxisDto { Rms=payload.z.rms, Crest=payload.z.crest, Bear=payload.z.bear, Gear=payload.z.gear, Freq=payload.z.f },
            X = new AxisDto { Rms=payload.x.rms, Crest=payload.x.crest, Bear=payload.x.bear, Gear=payload.x.gear, Freq=payload.x.f },
            Y = new AxisDto { Rms=payload.y.rms, Crest=payload.y.crest, Bear=payload.y.bear, Gear=payload.y.gear, Freq=payload.y.f },
            Alerts = alerts.Select(a => new AlertDto
            {
                Axis      = a.Axis.ToString(),
                Metric    = a.Metric.ToString(),
                Severity  = a.Severity.ToString(),
                Value     = a.Value,
                Threshold = a.Threshold
            }).ToList()
        };

        await _hub.Clients.All.SendAsync("ReceiveMeasurement", dto);

        _logger.LogInformation(
            "Published #{N} | Z rms={Zr:F4} crest={Zc:F2} | alerts={Count}",
            payload.n, payload.z.rms, payload.z.crest, alerts.Count);
    }
}