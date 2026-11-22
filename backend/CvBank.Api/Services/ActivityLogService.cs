using System.Globalization;
using CvBank.Api.Data;
using CvBank.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CvBank.Api.Services;

public class ActivityLogService(CvBankDbContext db)
{
    private static readonly ReportColumn[] Columns =
    [
        new("when", "When", "datetime"),
        new("user", "User", "text"),
        new("action", "Action", "text"),
        new("entityType", "Entity type", "text"),
        new("entityId", "Entity id", "text"),
        new("details", "Details", "text")
    ];

    public async Task<ActivityLogOptions> OptionsAsync()
    {
        var actors = await db.AuditLogs.AsNoTracking()
            .Where(a => a.UserDisplayName != null)
            .Select(a => a.UserDisplayName!)
            .Distinct()
            .OrderBy(name => name)
            .ToListAsync();

        var actions = await db.AuditLogs.AsNoTracking()
            .Select(a => a.Action)
            .Distinct()
            .OrderBy(action => action)
            .ToListAsync();

        var entityTypes = await db.AuditLogs.AsNoTracking()
            .Where(a => a.EntityType != null)
            .Select(a => a.EntityType!)
            .Distinct()
            .OrderBy(type => type)
            .ToListAsync();

        return new ActivityLogOptions(actors, actions, entityTypes);
    }

    public async Task<ReportData> BuildAsync(ActivityLogQuery query)
    {
        var matches = Filtered(query);

        var entries = await matches
            .OrderByDescending(a => a.OccurredAtUtc)
            .ThenByDescending(a => a.Id)
            .Take(Reports.RowLimit + 1)
            .Select(a => new Entry(a.OccurredAtUtc, a.UserDisplayName, a.Action, a.EntityType, a.EntityId, a.Details))
            .ToListAsync();

        var truncated = entries.Count > Reports.RowLimit;

        var rows = entries
            .Take(Reports.RowLimit)
            .Select(entry => (IReadOnlyList<object?>)new object?[]
            {
                Stamp(entry.OccurredAtUtc),
                entry.User ?? "—",
                entry.Action,
                entry.EntityType ?? "—",
                entry.EntityId ?? "—",
                entry.Details
            })
            .ToList();

        return new ReportData(
            "activity-log",
            "Activity Log",
            "Every action recorded in the system, newest first. Times are shown in the server's local time.",
            Summary(query),
            Columns,
            rows,
            truncated);
    }

    private IQueryable<AuditLog> Filtered(ActivityLogQuery query)
    {
        IQueryable<AuditLog> rows = db.AuditLogs.AsNoTracking();

        if (query.DateFrom is { } dateFrom)
        {
            var from = UtcStart(dateFrom);
            rows = rows.Where(a => a.OccurredAtUtc >= from);
        }

        if (query.DateTo is { } dateTo)
        {
            var to = UtcEnd(dateTo);
            rows = rows.Where(a => a.OccurredAtUtc <= to);
        }

        if (Trim(query.Actor) is { } actor)
        {
            rows = rows.Where(a => a.UserDisplayName == actor);
        }

        if (Trim(query.Action) is { } action)
        {
            rows = rows.Where(a => a.Action == action);
        }

        if (Trim(query.EntityType) is { } entityType)
        {
            rows = rows.Where(a => a.EntityType == entityType);
        }

        if (Trim(query.Search) is { } search)
        {
            rows = rows.Where(a =>
                a.Action.Contains(search) ||
                (a.Details != null && a.Details.Contains(search)) ||
                (a.UserDisplayName != null && a.UserDisplayName.Contains(search)) ||
                (a.EntityId != null && a.EntityId.Contains(search)));
        }

        return rows;
    }

    private static string Summary(ActivityLogQuery query)
    {
        var parts = new List<string>
        {
            query.DateFrom is { } from && query.DateTo is { } to ? $"From {Label(from)} to {Label(to)}"
                : query.DateFrom is { } oneFrom ? $"From {Label(oneFrom)}"
                : query.DateTo is { } oneTo ? $"Up to {Label(oneTo)}"
                : "All dates"
        };

        if (Trim(query.Actor) is { } actor)
        {
            parts.Add($"User: {actor}");
        }

        if (Trim(query.Action) is { } action)
        {
            parts.Add($"Action: {action}");
        }

        if (Trim(query.EntityType) is { } entityType)
        {
            parts.Add($"Entity: {entityType}");
        }

        if (Trim(query.Search) is { } search)
        {
            parts.Add($"Search: '{search}'");
        }

        return string.Join("  ·  ", parts);
    }

    private static DateTime UtcStart(DateOnly day) =>
        TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(day.ToDateTime(new TimeOnly()), DateTimeKind.Unspecified), TimeZoneInfo.Local);

    private static DateTime UtcEnd(DateOnly day) =>
        TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(day.ToDateTime(new TimeOnly(23, 59, 59)), DateTimeKind.Unspecified), TimeZoneInfo.Local);

    private static string Stamp(DateTime occurredAtUtc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(occurredAtUtc, DateTimeKind.Utc), TimeZoneInfo.Local)
            .ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);

    private static string Label(DateOnly value) => value.ToString("dd MMM yyyy", CultureInfo.InvariantCulture);

    private static string? Trim(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private sealed record Entry(
        DateTime OccurredAtUtc,
        string? User,
        string Action,
        string? EntityType,
        string? EntityId,
        string? Details);
}
