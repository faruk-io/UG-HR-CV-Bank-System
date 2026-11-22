using CvBank.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace CvBank.Api.Services;

public static class SettingKeys
{
    public const string CompanyName = "company.name";
    public const string CompanyAddress = "company.address";
    public const string MaxUploadMb = "upload.max_size_mb";

    public const string DefaultCompanyName = "Union Group CV Bank";
    public const int DefaultMaxUploadMb = 10;
    public const int MinMaxUploadMb = 1;
    public const int MaxMaxUploadMb = 20;

    public static readonly (string Key, string DefaultValue, string Description)[] All =
    [
        (CompanyName, DefaultCompanyName, "Shown in the side menu and printed above the candidate summary."),
        (CompanyAddress, string.Empty, "Second printed line of the candidate summary. Leave blank to hide it."),
        (MaxUploadMb, DefaultMaxUploadMb.ToString(), "Largest CV file or scanned evaluation form the server accepts, in MB.")
    ];
}

/// <summary>
/// Settings live in the SystemSettings table but are read by singletons and by request code,
/// so the rows are held in one in-memory snapshot that is replaced atomically after every save.
/// </summary>
public sealed class SettingsCache(IServiceScopeFactory scopeFactory)
{
    private static readonly IReadOnlyDictionary<string, string> Empty =
        new Dictionary<string, string>(StringComparer.Ordinal);

    private volatile IReadOnlyDictionary<string, string> _values = Empty;

    public string CompanyName => Value(SettingKeys.CompanyName) is { Length: > 0 } name
        ? name
        : SettingKeys.DefaultCompanyName;

    public string? CompanyAddress => Value(SettingKeys.CompanyAddress) is { Length: > 0 } address ? address : null;

    public int? MaxUploadMb => int.TryParse(Value(SettingKeys.MaxUploadMb), out var mb)
        && mb is >= SettingKeys.MinMaxUploadMb and <= SettingKeys.MaxMaxUploadMb
        ? mb
        : null;

    public long? MaxUploadBytes => MaxUploadMb is { } mb ? mb * 1024L * 1024L : null;

    public string? Value(string key) => _values.TryGetValue(key, out var value) ? value : null;

    public async Task RefreshAsync()
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CvBankDbContext>();

        var loaded = await db.SystemSettings
            .Select(s => new { s.Key, s.Value })
            .ToDictionaryAsync(x => x.Key, x => x.Value ?? string.Empty, StringComparer.Ordinal);

        _values = loaded;
    }
}
