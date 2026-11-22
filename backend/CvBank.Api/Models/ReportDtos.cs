using CvBank.Api.Services;

namespace CvBank.Api.Models;

public static class ReportFilter
{
    public const string DateRange = "dateRange";
    public const string Search = "search";
    public const string JobPosition = "jobPosition";
    public const string Concern = "concern";
    public const string Status = "status";
    public const string CvOnFile = "cvOnFile";
    public const string GroupBy = "groupBy";
    public const string InterviewView = "interviewView";
}

public static class ReportOptions
{
    public static readonly (string Key, string Label)[] GroupDimensions =
    [
        ("organization", "Organization"),
        ("designation", "Designation"),
        ("position", "Applied Position"),
        ("concern", "Concern Name"),
        ("status", "Candidate status"),
        ("registered-date", "Registration date")
    ];

    public static readonly (string Key, string Label)[] CvOnFile =
    [
        ("all", "All candidates"),
        ("yes", "With CV on file"),
        ("no", "Without CV on file")
    ];

    public static readonly (string Key, string Label)[] InterviewViews =
    [
        ("detail", "Every interview (row per interview)"),
        ("date", "By interview date (daily summary)")
    ];

    public static string DimensionLabel(string key) =>
        GroupDimensions.FirstOrDefault(d => d.Key == key).Label ?? "Organization";

    public static bool IsDimension(string key) => GroupDimensions.Any(d => d.Key == key);
}

public sealed record ReportColumn(string Key, string Label, string Type);

public sealed record ReportDefinition(
    string Code,
    string Title,
    string Description,
    IReadOnlyList<string> Filters,
    bool InMenu);

public sealed record ReportData(
    string Code,
    string Title,
    string Description,
    string FilterSummary,
    IReadOnlyList<ReportColumn> Columns,
    IReadOnlyList<IReadOnlyList<object?>> Rows,
    bool Truncated);

public sealed class ReportQuery
{
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }
    public string? Search { get; set; }
    public int? JobPositionId { get; set; }
    public int? ConcernId { get; set; }
    public CandidateStatus? Status { get; set; }

    public string CvOnFile { get; set; } = "all";

    public string GroupBy { get; set; } = "organization";

    public string InterviewView { get; set; } = "detail";

    public bool HasCvOnly => string.Equals(CvOnFile, "yes", StringComparison.OrdinalIgnoreCase);

    public bool HasNoCv => string.Equals(CvOnFile, "no", StringComparison.OrdinalIgnoreCase);

    public string NormalizedGroupBy => ReportOptions.IsDimension(GroupBy) ? GroupBy : "organization";
}

public static class Reports
{
    public const int RowLimit = 2000;

    public static readonly IReadOnlyList<ReportDefinition> All =
    [
        new ReportDefinition(
            "candidate-report",
            "Candidate Report",
            "One row per candidate, with CV, interview count, status and remarks. Filter by CV on file and status.",
            [ReportFilter.DateRange, ReportFilter.Search, ReportFilter.JobPosition, ReportFilter.Concern, ReportFilter.Status, ReportFilter.CvOnFile],
            true),
        new ReportDefinition(
            "group-by-candidates",
            "Candidate Groups",
            "Count of candidates per group. Choose what to group by.",
            [ReportFilter.DateRange, ReportFilter.Search, ReportFilter.JobPosition, ReportFilter.Concern, ReportFilter.Status, ReportFilter.CvOnFile, ReportFilter.GroupBy],
            true),
        new ReportDefinition(
            "interview-report",
            "Interview Report",
            "Interviews held — every interview as its own row, or summed per interview date.",
            [ReportFilter.DateRange, ReportFilter.Search, ReportFilter.JobPosition, ReportFilter.Concern, ReportFilter.InterviewView],
            true),

        // Kept working by code so older exports and bookmarks still run; hidden from the report list.
        new ReportDefinition(
            "total-cv",
            "Total CV Report",
            "Every candidate that has a current CV on file.",
            [ReportFilter.DateRange, ReportFilter.Search, ReportFilter.JobPosition, ReportFilter.Concern, ReportFilter.Status],
            false),
        new ReportDefinition(
            "total-candidate",
            "Total Candidate Report",
            "Every candidate registered in the CV Bank.",
            [ReportFilter.DateRange, ReportFilter.Search, ReportFilter.JobPosition, ReportFilter.Concern, ReportFilter.Status],
            false),
        new ReportDefinition(
            "date-wise-candidate",
            "Date-wise Candidate Report",
            "Candidates registered day by day, with how many hold a CV.",
            [ReportFilter.DateRange, ReportFilter.Search, ReportFilter.JobPosition, ReportFilter.Concern, ReportFilter.Status],
            false),
        new ReportDefinition(
            "interview-history",
            "Interview History Report",
            "Every interview held, with rating, status and decision.",
            [ReportFilter.DateRange, ReportFilter.Search, ReportFilter.JobPosition, ReportFilter.Concern],
            false),
        new ReportDefinition(
            "selected-candidate",
            "Selected Candidate Report",
            "Candidates whose status is Selected.",
            [ReportFilter.DateRange, ReportFilter.Search, ReportFilter.JobPosition, ReportFilter.Concern],
            false),
        new ReportDefinition(
            "organization-wise",
            "Organization-wise Candidate Report",
            "Candidates grouped by the organization they currently work for.",
            [ReportFilter.DateRange, ReportFilter.Search, ReportFilter.JobPosition, ReportFilter.Concern],
            false),
        new ReportDefinition(
            "designation-wise",
            "Designation-wise Candidate Report",
            "Candidates grouped by the designation they currently hold.",
            [ReportFilter.DateRange, ReportFilter.Search, ReportFilter.JobPosition, ReportFilter.Concern],
            false),
        new ReportDefinition(
            "job-position-wise",
            "Job Position-wise Candidate Report",
            "Candidates grouped by the position they applied for.",
            [ReportFilter.DateRange, ReportFilter.Search, ReportFilter.Concern],
            false),
        new ReportDefinition(
            "interview-date-wise",
            "Interview Date-wise Report",
            "Interviews held day by day, with average rating and decisions.",
            [ReportFilter.DateRange, ReportFilter.Search, ReportFilter.JobPosition, ReportFilter.Concern],
            false)
    ];

    public static readonly IReadOnlyList<ReportDefinition> Menu = All.Where(definition => definition.InMenu).ToList();

    public static ReportDefinition Find(string code) =>
        All.FirstOrDefault(definition => string.Equals(definition.Code, code, StringComparison.OrdinalIgnoreCase))
        ?? throw new NotFoundException($"Report '{code}' was not found.");

    public static string StatusLabel(CandidateStatus status) => status switch
    {
        CandidateStatus.Shortlisted => "Short-listed",
        CandidateStatus.OptionalBackup => "Optional (Back up)",
        CandidateStatus.OverBudget => "Over Budget",
        CandidateStatus.Selected => "Selected",
        CandidateStatus.Fresher => "Fresher",
        _ => status.ToString()
    };
}
