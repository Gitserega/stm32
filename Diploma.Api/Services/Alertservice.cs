using Diploma.Api.Models;
using Diploma.Entity;
using Microsoft.EntityFrameworkCore;

namespace Diploma.Api.Services;

public class AlertService
{
    private readonly AppDbContext _db;

    public AlertService(AppDbContext db) => _db = db;

    /// <summary>
    /// Проверяет признаки против порогов из БД для конкретного устройства.
    /// Возвращает список созданных алертов (уже сохранённых).
    /// </summary>
    public async Task<List<Alert>> CheckAndSaveAsync(
        Measurement measurement,
        MqttPayload payload,
        CancellationToken ct = default)
    {
        // Загружаем пороги для конкретного устройства
        var thresholds = await _db.Thresholds
            .AsNoTracking()
            .Where(t => t.DeviceId == measurement.DeviceId)
            .ToDictionaryAsync(t => t.Metric, t => t.Value, ct);

        // Если порогов для устройства нет — используем дефолтные значения
        double crestThr = thresholds.GetValueOrDefault("crest", 4.0);
        double bearingThr = thresholds.GetValueOrDefault("bearing", 0.05);
        double gearThr = thresholds.GetValueOrDefault("gear", 0.05);

        var alerts = new List<Alert>();

        var axes = new[]
        {
            (Axis: AlertAxis.Z, Data: payload.z),
            (Axis: AlertAxis.X, Data: payload.x),
            (Axis: AlertAxis.Y, Data: payload.y),
        };

        foreach (var (axis, data) in axes)
        {
            if (data.crest > crestThr)
                alerts.Add(MakeAlert(measurement.Id, axis, AlertMetric.Crest, data.crest, crestThr));

            if (data.bear > bearingThr)
                alerts.Add(MakeAlert(measurement.Id, axis, AlertMetric.Bearing, data.bear, bearingThr));

            if (data.gear > gearThr)
                alerts.Add(MakeAlert(measurement.Id, axis, AlertMetric.Gear, data.gear, gearThr));
        }

        if (alerts.Count > 0)
        {
            _db.Alerts.AddRange(alerts);
            await _db.SaveChangesAsync(ct);
        }

        return alerts;
    }

    private static Alert MakeAlert(
        long measurementId, AlertAxis axis, AlertMetric metric,
        double value, double threshold)
    {
        var severity = value > threshold * 2.0
            ? AlertSeverity.Critical
            : AlertSeverity.Warning;

        return new Alert
        {
            TriggeredAt = DateTime.UtcNow,
            MeasurementId = measurementId,
            // DeviceId убран из Alert — берётся через Measurement.DeviceId при необходимости
            Axis = axis,
            Metric = metric,
            Severity = severity,
            Value = value,
            Threshold = threshold
        };
    }
}