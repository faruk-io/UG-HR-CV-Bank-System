namespace CvBank.Api.Models;

public sealed record SettingsDto(
    string CompanyName,
    string? CompanyAddress,
    int MaxUploadMb,
    string? UpdatedByName,
    DateTime? UpdatedAtUtc);

public class SettingsUpdateRequest
{
    public string? CompanyName { get; set; }

    public string? CompanyAddress { get; set; }

    public int? MaxUploadMb { get; set; }
}
