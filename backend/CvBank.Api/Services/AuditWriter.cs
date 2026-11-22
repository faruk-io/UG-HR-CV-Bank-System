using CvBank.Api.Data;
using CvBank.Api.Models;

namespace CvBank.Api.Services;

public interface IAuditWriter
{
    Task WriteAsync(int? userId, string? userDisplayName, string action, string? entityType, string? entityId, string? details);
}

public class AuditWriter(CvBankDbContext db) : IAuditWriter
{
    public async Task WriteAsync(int? userId, string? userDisplayName, string action, string? entityType, string? entityId, string? details)
    {
        db.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            UserDisplayName = userDisplayName,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Details = details,
            OccurredAtUtc = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
    }
}
