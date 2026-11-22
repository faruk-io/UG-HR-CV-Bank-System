using CvBank.Api.Auth;
using CvBank.Api.Data;
using CvBank.Api.Models;
using CvBank.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CvBank.Api.Controllers;

[ApiController]
[Route("api/settings")]
public class SettingsController(
    CvBankDbContext db,
    SettingsCache settings,
    IAuditWriter audit) : ControllerBase
{
    private static readonly string[] Keys = [.. SettingKeys.All.Select(s => s.Key)];

    private SignedInUser Me => SignedInUser.From(User);

    /// <summary>
    /// The values every signed-in screen needs (brand name, upload limit shown next to file pickers).
    /// Editable only with the settings.manage permission.
    /// </summary>
    [HttpGet("display")]
    [Authorize]
    public ActionResult<SettingsDto> Display()
    {
        return Ok(new SettingsDto(
            settings.CompanyName,
            settings.CompanyAddress,
            settings.MaxUploadMb ?? SettingKeys.DefaultMaxUploadMb,
            null,
            null));
    }

    [HttpGet]
    [Permission(Permissions.SettingsManage)]
    public async Task<ActionResult<SettingsDto>> Read()
    {
        return Ok(await ReadAsync());
    }

    [HttpPut]
    [Permission(Permissions.SettingsManage)]
    public async Task<ActionResult<SettingsDto>> Update(SettingsUpdateRequest request)
    {
        var current = await ReadAsync();

        var companyName = Require(
            request.CompanyName ?? current.CompanyName,
            "Company name",
            200);

        var companyAddress = Trim(request.CompanyAddress ?? current.CompanyAddress, 500, "Address");

        var maxUploadMb = request.MaxUploadMb ?? current.MaxUploadMb;

        if (maxUploadMb is < SettingKeys.MinMaxUploadMb or > SettingKeys.MaxMaxUploadMb)
        {
            throw new DomainException(
                $"The upload limit must be between {SettingKeys.MinMaxUploadMb} and {SettingKeys.MaxMaxUploadMb} MB.");
        }

        var changes = new List<string>();

        if (!string.Equals(current.CompanyName, companyName, StringComparison.Ordinal))
        {
            changes.Add($"Company name: \"{current.CompanyName}\" to \"{companyName}\"");
        }

        if (!string.Equals(current.CompanyAddress ?? string.Empty, companyAddress, StringComparison.Ordinal))
        {
            changes.Add(string.IsNullOrEmpty(companyAddress)
                ? "Company address cleared"
                : $"Company address: \"{companyAddress}\"");
        }

        if (current.MaxUploadMb != maxUploadMb)
        {
            changes.Add($"Upload limit: {current.MaxUploadMb} MB to {maxUploadMb} MB");
        }

        if (changes.Count == 0)
        {
            return Ok(current);
        }

        var rows = await db.SystemSettings.Where(s => Keys.Contains(s.Key)).ToListAsync();

        foreach (var (key, value) in NewValues(companyName, companyAddress, maxUploadMb))
        {
            var row = rows.FirstOrDefault(r => string.Equals(r.Key, key, StringComparison.Ordinal));

            if (row is null)
            {
                row = new SystemSetting
                {
                    Key = key,
                    Value = value,
                    Description = DescriptionFor(key),
                    UpdatedAtUtc = DateTime.UtcNow,
                    UpdatedById = Me.UserId
                };
                db.SystemSettings.Add(row);
            }
            else if (!string.Equals(row.Value ?? string.Empty, value, StringComparison.Ordinal))
            {
                row.Value = value;
                row.UpdatedAtUtc = DateTime.UtcNow;
                row.UpdatedById = Me.UserId;
            }
        }

        await db.SaveChangesAsync();

        // Every reader of the cached snapshot, including the upload limit inside file storage,
        // must see the saved value on the next request rather than after a restart.
        await settings.RefreshAsync();

        await audit.WriteAsync(
            Me.UserId,
            Me.FullName,
            "Settings Update",
            "SystemSetting",
            null,
            string.Join("  ·  ", changes));

        return Ok(await ReadAsync());
    }

    private static IEnumerable<(string Key, string Value)> NewValues(string companyName, string companyAddress, int maxUploadMb) =>
    [
        (SettingKeys.CompanyName, companyName),
        (SettingKeys.CompanyAddress, companyAddress),
        (SettingKeys.MaxUploadMb, maxUploadMb.ToString())
    ];

    private static string? DescriptionFor(string key) =>
        SettingKeys.All.FirstOrDefault(row => row.Key == key).Description;

    private async Task<SettingsDto> ReadAsync()
    {
        var rows = await db.SystemSettings
            .Where(s => Keys.Contains(s.Key))
            .Select(s => new SettingRow(s.Key, s.Value, s.UpdatedAtUtc, s.UpdatedBy!.FullName))
            .ToListAsync();

        var last = rows.OrderByDescending(r => r.UpdatedAtUtc).FirstOrDefault();

        return new SettingsDto(
            Value(rows, SettingKeys.CompanyName, SettingKeys.DefaultCompanyName),
            EmptyToNull(Value(rows, SettingKeys.CompanyAddress, string.Empty)),
            int.TryParse(Value(rows, SettingKeys.MaxUploadMb, null), out var mb)
                ? mb
                : SettingKeys.DefaultMaxUploadMb,
            last?.UpdatedByName,
            last?.UpdatedAtUtc);
    }

    private static string Value(List<SettingRow> rows, string key, string? fallback) =>
        rows.FirstOrDefault(r => string.Equals(r.Key, key, StringComparison.Ordinal))?.Value ?? fallback ?? string.Empty;

    private static string? EmptyToNull(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string Trim(string? value, int maxLength, string label)
    {
        var trimmed = (value ?? string.Empty).Trim();

        if (trimmed.Length > maxLength)
        {
            throw new DomainException($"The {label} is longer than {maxLength} characters.");
        }

        return trimmed;
    }

    private static string Require(string? value, string label, int maxLength)
    {
        var trimmed = Trim(value, maxLength, label);

        if (trimmed.Length == 0)
        {
            throw new DomainException($"{label} is required.");
        }

        return trimmed;
    }

    private sealed record SettingRow(string Key, string? Value, DateTime UpdatedAtUtc, string? UpdatedByName);
}
