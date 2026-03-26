using Diploma.Api.Models;
using Diploma.Entity;
using Microsoft.EntityFrameworkCore;

namespace Diploma.Api.Services;

public class AlertService
{
    private readonly AppDbContext _db;
 
    public AlertService(AppDbContext db)
    {
        _db = db;
    }
 
    /* Проверяет признаки против порогов из БД.
     * Возвращает список созданных алертов (уже сохранённых). */
    public async Task<List<Alert>> CheckAndSaveAsync(
        Measurement measurement,
        MqttPayload payload,
        CancellationToken ct = default)
    {
        /* Загружаем пороги один раз — кешируем в словарь */
        var thresholds = await _db.Thresholds
            .AsNoTracking()
            .ToDictionaryAsync(t => t.Metric, t => t.Value, ct);
 
        var alerts = new List<Alert>();
 
        double crestThreshold   = thresholds.GetValueOrDefault("crest",   4.0);
        double bearingThreshold = thresholds.GetValueOrDefault("bearing", 0.05);
        double gearThreshold    = thresholds.GetValueOrDefault("gear",    0.05);
 
        /* Проверяем каждую ось */
        var axes = new[]
        {
            (Axis: AlertAxis.Z, Data: payload.z),
            (Axis: AlertAxis.X, Data: payload.x),
            (Axis: AlertAxis.Y, Data: payload.y),
        };
 
        foreach (var (axis, data) in axes)
        {
            /* Crest factor */
            if (data.crest > crestThreshold)
                alerts.Add(MakeAlert(measurement.Id, axis, AlertMetric.Crest,
                    data.crest, crestThreshold));
 
            /* Bearing energy */
            if (data.bear > bearingThreshold)
                alerts.Add(MakeAlert(measurement.Id, axis, AlertMetric.Bearing,
                    data.bear, bearingThreshold));
 
            /* Gear energy */
            if (data.gear > gearThreshold)
                alerts.Add(MakeAlert(measurement.Id, axis, AlertMetric.Gear,
                    data.gear, gearThreshold));
        }
 
        if (alerts.Count > 0)
        {
            _db.Alerts.AddRange(alerts);
            await _db.SaveChangesAsync(ct);
        }
 
        return alerts;
    }
 
    private static Alert MakeAlert(
        long measurementId,
        AlertAxis axis,
        AlertMetric metric,
        double value,
        double threshold)
    {
        /* Severity: Critical если значение > 2× порога, иначе Warning */
        var severity = value > threshold * 2.0
            ? AlertSeverity.Critical
            : AlertSeverity.Warning;
 
        return new Alert
        {
            TriggeredAt   = DateTime.UtcNow,
            MeasurementId = measurementId,
            Axis          = axis,
            Metric        = metric,
            Severity      = severity,
            Value         = value,
            Threshold     = threshold
        };
    }
}