using CvBank.Api.Models;
using CvBank.Api.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CvBank.Api.Data;

public static class DbInitializer
{
    private static readonly string[] ExecutivePermissions =
    [
        Permissions.CandidateView,
        Permissions.CandidateCreate,
        Permissions.CandidateUpdate,
        Permissions.CandidateDelete,
        Permissions.CvUpload,
        Permissions.CvDownload,
        Permissions.EvaluationView,
        Permissions.EvaluationGenerate,
        Permissions.EvaluationUpload
    ];

    private static readonly string[] ManagerPermissions =
        [.. ExecutivePermissions, Permissions.EvaluationManage, Permissions.ReportView];

    public static async Task InitializeAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<CvBankDbContext>();
        var hasher = services.GetRequiredService<IPasswordHasher<User>>();
        var configuration = services.GetRequiredService<IConfiguration>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(DbInitializer));

        await db.Database.MigrateAsync();

        if (!await db.Roles.AnyAsync())
        {
            db.Roles.AddRange(
                new Role { Code = RoleCodes.Executive, Name = "HR Executive" },
                new Role { Code = RoleCodes.Manager, Name = "HR Manager" },
                new Role { Code = RoleCodes.SuperAdmin, Name = "Super Admin" });

            await db.SaveChangesAsync();

            var rolePermissions = new Dictionary<string, string[]>
            {
                [RoleCodes.Executive] = ExecutivePermissions,
                [RoleCodes.Manager] = ManagerPermissions,
                [RoleCodes.SuperAdmin] = AllPermissionCodes()
            };

            foreach (var role in await db.Roles.ToListAsync())
            {
                db.RolePermissions.AddRange(
                    rolePermissions[role.Code].Select(code => new RolePermission { RoleId = role.Id, PermissionCode = code }));
            }

            await db.SaveChangesAsync();
        }

        if (!await db.Users.AnyAsync())
        {
            var adminPassword = configuration["Seed:AdminPassword"];
            var temporaryPassword = string.IsNullOrWhiteSpace(adminPassword) ? "Admin@123" : adminPassword;

            var superAdminRole = await db.Roles.SingleAsync(r => r.Code == RoleCodes.SuperAdmin);
            var admin = new User
            {
                FullName = "System Administrator",
                UserName = "admin",
                Email = null,
                PasswordHash = string.Empty,
                RoleId = superAdminRole.Id,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            };
            admin.PasswordHash = hasher.HashPassword(admin, temporaryPassword);

            db.Users.Add(admin);
            await db.SaveChangesAsync();

            if (string.IsNullOrWhiteSpace(adminPassword))
            {
                logger.LogWarning(
                    "Seeded user 'admin' with the built-in temporary password. Change it from User Management before real use.");
            }
        }

        await SeedSettingsAsync(db);
        await services.GetRequiredService<SettingsCache>().RefreshAsync();
    }

    /// <summary>
    /// Creates the setting rows the app reads so the Super Admin only has to edit values, never invent keys.
    /// Existing rows keep their values — an operator's change is never overwritten by a restart.
    /// </summary>
    private static async Task SeedSettingsAsync(CvBankDbContext db)
    {
        var existing = await db.SystemSettings.Select(s => s.Key).ToListAsync();

        var missing = SettingKeys.All
            .Where(row => !existing.Contains(row.Key, StringComparer.Ordinal))
            .Select(row => new SystemSetting
            {
                Key = row.Key,
                Value = row.DefaultValue,
                Description = row.Description,
                UpdatedAtUtc = DateTime.UtcNow
            })
            .ToList();

        if (missing.Count > 0)
        {
            db.SystemSettings.AddRange(missing);
            await db.SaveChangesAsync();
        }
    }

    private static string[] AllPermissionCodes() =>
    [
        Permissions.CandidateView,
        Permissions.CandidateCreate,
        Permissions.CandidateUpdate,
        Permissions.CandidateDelete,
        Permissions.CvUpload,
        Permissions.CvDownload,
        Permissions.EvaluationView,
        Permissions.EvaluationGenerate,
        Permissions.EvaluationUpload,
        Permissions.EvaluationManage,
        Permissions.ReportView,
        Permissions.UserManage,
        Permissions.SettingsManage,
        Permissions.ActivityLogView,
        Permissions.DocumentManage
    ];
}
