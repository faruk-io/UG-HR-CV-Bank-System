namespace CvBank.Api.Models;

public sealed class ActivityLogQuery
{
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }
    public string? Actor { get; set; }
    public string? Action { get; set; }
    public string? EntityType { get; set; }
    public string? Search { get; set; }
}

public sealed record ActivityLogOptions(
    IReadOnlyList<string> Actors,
    IReadOnlyList<string> Actions,
    IReadOnlyList<string> EntityTypes);
