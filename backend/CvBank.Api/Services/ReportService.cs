using CvBank.Api.Data;
using CvBank.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CvBank.Api.Services;

public class ReportService(CvBankDbContext db)
{
    private static readonly ReportColumn[] CandidateColumns =
    [
        new("code", "Candidate code", "text"),
        new("name", "Name", "text"),
        new("mobile", "Mobile", "text"),
        new("email", "Email", "text"),
        new("position", "Applied Position", "text"),
        new("concern", "Concern Name", "text"),
        new("organization", "Organization", "text"),
        new("designation", "Designation", "text"),
        new("expected", "Expected salary", "money"),
        new("cv", "CV on file", "text"),
        new("interviews", "Interviews", "number"),
        new("status", "Status", "text"),
        new("registered", "Registered on", "date"),
        new("remarks", "Remarks", "text")
    ];

    public async Task<ReportData> BuildAsync(string code, ReportQuery query)
    {
        var definition = Reports.Find(code);
        var filter = await FilterSummaryAsync(definition, query);

        ValueTuple<ReportColumn[], IReadOnlyList<IReadOnlyList<object?>>> shaped = definition.Code switch
        {
            "candidate-report" => (CandidateColumns, await CandidateRowsAsync(query, status: null)),
            "group-by-candidates" => (
                GroupColumns(ReportOptions.DimensionLabel(query.NormalizedGroupBy)),
                await GroupRowsAsync(query, query.NormalizedGroupBy)),
            "interview-report" when query.InterviewView.Equals("date", StringComparison.OrdinalIgnoreCase) =>
                (InterviewDateWiseColumns, await InterviewDateWiseRowsAsync(query)),
            "interview-report" => (InterviewHistoryColumns, await InterviewHistoryRowsAsync(query)),
            "total-cv" => (CandidateColumns, await CvRowsAsync(query)),
            "total-candidate" => (CandidateColumns, await CandidateRowsAsync(query, status: null)),
            "date-wise-candidate" => (DateWiseCandidateColumns, await DateWiseCandidateRowsAsync(query)),
            "interview-history" => (InterviewHistoryColumns, await InterviewHistoryRowsAsync(query)),
            "selected-candidate" => (CandidateColumns, await CandidateRowsAsync(query, CandidateStatus.Selected)),
            "organization-wise" => (GroupColumns("Organization"), await GroupRowsAsync(query, "organization")),
            "designation-wise" => (GroupColumns("Designation"), await GroupRowsAsync(query, "designation")),
            "job-position-wise" => (GroupColumns("Applied Position"), await GroupRowsAsync(query, "position")),
            "interview-date-wise" => (InterviewDateWiseColumns, await InterviewDateWiseRowsAsync(query)),
            _ => throw new NotFoundException($"Report '{definition.Code}' is not implemented.")
        };

        var (columns, rows) = shaped;

        return new ReportData(
            definition.Code,
            definition.Title,
            definition.Description,
            filter,
            columns,
            rows,
            rows.Count >= Reports.RowLimit);
    }

    private async Task<IReadOnlyList<IReadOnlyList<object?>>> CvRowsAsync(ReportQuery query)
    {
        var candidates = await LoadCandidatesAsync(query);

        return Shape(candidates.Where(c => c.HasCv));
    }

    private async Task<IReadOnlyList<IReadOnlyList<object?>>> CandidateRowsAsync(ReportQuery query, CandidateStatus? status)
    {
        var candidates = await LoadCandidatesAsync(query);

        if (status is { } wanted)
        {
            candidates = candidates.Where(c => c.Status == wanted).ToList();
        }

        return Shape(candidates);
    }

    private static IReadOnlyList<IReadOnlyList<object?>> Shape(IEnumerable<CandidateRow> candidates) =>
        candidates
            .OrderByDescending(c => c.RegisteredOn)
            .Take(Reports.RowLimit)
            .Select(c => new object?[]
            {
                c.Code,
                c.Name,
                c.Mobile,
                c.Email,
                c.Position,
                c.Concern,
                c.Organization,
                c.Designation,
                c.ExpectedSalary,
                c.HasCv ? c.CvFileName : "No",
                c.Interviews,
                Reports.StatusLabel(c.Status),
                Day(c.RegisteredOn),
                c.Remarks
            })
            .ToList();

    private static readonly ReportColumn[] DateWiseCandidateColumns =
    [
        new("date", "Date", "date"),
        new("registered", "Candidates registered", "number"),
        new("withCv", "With CV", "number"),
        new("selected", "Selected", "number")
    ];

    private async Task<IReadOnlyList<IReadOnlyList<object?>>> DateWiseCandidateRowsAsync(ReportQuery query)
    {
        var candidates = await LoadCandidatesAsync(query);

        return candidates
            .GroupBy(c => Day(c.RegisteredOn))
            .OrderByDescending(g => g.Key)
            .Take(Reports.RowLimit)
            .Select(g => new object?[]
            {
                g.Key,
                g.Count(),
                g.Count(c => c.HasCv),
                g.Count(c => c.Status == CandidateStatus.Selected)
            })
            .ToList();
    }

    private static readonly ReportColumn[] InterviewHistoryColumns =
    [
        new("code", "Candidate code", "text"),
        new("name", "Candidate name", "text"),
        new("interview", "Interview #", "number"),
        new("date", "Interview date", "date"),
        new("position", "Applied Position", "text"),
        new("rating", "Rating", "number"),
        new("status", "Interview status", "text"),
        new("decision", "Decision", "text"),
        new("recommendation", "Recommendation", "text"),
        new("filledBy", "Filled by", "text")
    ];

    private async Task<IReadOnlyList<IReadOnlyList<object?>>> InterviewHistoryRowsAsync(ReportQuery query)
    {
        var interviews = await LoadInterviewsAsync(query);

        return interviews
            .OrderByDescending(i => i.InterviewDate)
            .ThenByDescending(i => i.Id)
            .Take(Reports.RowLimit)
            .Select(i => new object?[]
            {
                i.Code,
                i.Name,
                i.InterviewNumber,
                Day(i.InterviewDate),
                i.Position,
                i.TotalRating,
                InterviewStatusLabel(i.Status),
                DecisionLabel(i.Decision),
                i.Recommendation,
                i.CreatedByName
            })
            .ToList();
    }

    private static ReportColumn[] GroupColumns(string dimension) =>
    [
        new("group", dimension, "text"),
        new("candidates", "Candidates", "number"),
        new("withCv", "With CV", "number"),
        new("selected", "Selected", "number"),
        new("interviews", "Interviews held", "number"),
        new("experience", "Avg experience (yrs)", "decimal")
    ];

    private async Task<IReadOnlyList<IReadOnlyList<object?>>> GroupRowsAsync(
        ReportQuery query,
        string dimension)
    {
        Func<CandidateRow, string?> selector = dimension switch
        {
            "designation" => c => c.Designation,
            "position" => c => c.Position,
            "concern" => c => c.Concern,
            "status" => c => Reports.StatusLabel(c.Status),
            "registered-date" => c => Day(c.RegisteredOn),
            _ => c => c.Organization
        };

        var candidates = await LoadCandidatesAsync(query);

        var groups = candidates
            .GroupBy(c => selector(c) is { Length: > 0 } value ? value.Trim() : "(not set)")
            .Select(g => new
            {
                Group = g.Key,
                Cells = new object?[]
                {
                    g.Key,
                    g.Count(),
                    g.Count(c => c.HasCv),
                    g.Count(c => c.Status == CandidateStatus.Selected),
                    g.Sum(c => c.Interviews),
                    AverageYears(g)
                }
            })
            .ToList();

        // Days read newest first; every other dimension is ranked by size.
        var ordered = dimension == "registered-date"
            ? groups.OrderByDescending(x => (string)x.Cells[0]!)
            : groups.OrderByDescending(x => (int)x.Cells[1]!).ThenBy(x => x.Group);

        return ordered.Take(Reports.RowLimit).Select(x => x.Cells).ToList();
    }

    private static readonly ReportColumn[] InterviewDateWiseColumns =
    [
        new("date", "Date", "date"),
        new("interviews", "Interviews held", "number"),
        new("candidates", "Candidates seen", "number"),
        new("avgRating", "Avg rating", "decimal"),
        new("selected", "Selected", "number"),
        new("pending", "Still pending", "number")
    ];

    private async Task<IReadOnlyList<IReadOnlyList<object?>>> InterviewDateWiseRowsAsync(ReportQuery query)
    {
        var interviews = await LoadInterviewsAsync(query);

        return interviews
            .GroupBy(i => Day(i.InterviewDate))
            .OrderByDescending(g => g.Key)
            .Take(Reports.RowLimit)
            .Select(g => new object?[]
            {
                g.Key,
                g.Count(),
                g.Select(i => i.Code).Distinct().Count(),
                Math.Round(g.Average(i => (double)i.TotalRating), 1),
                g.Count(i => i.Decision == InterviewDecision.Selected),
                g.Count(i => i.Status == InterviewStatus.Pending)
            })
            .ToList();
    }

    private async Task<List<CandidateRow>> LoadCandidatesAsync(ReportQuery query)
    {
        var rows = db.Candidates.AsNoTracking().AsQueryable();
        rows = ApplyCandidateFilters(rows, query);

        var cvWanted = query.HasCvOnly ? true : query.HasNoCv ? false : (bool?)null;
        if (cvWanted is { } wanted)
        {
            rows = rows.Where(c => c.Documents.Any(d => d.Category == DocumentCategory.Cv && d.IsCurrent) == wanted);
        }

        return await rows
            .OrderByDescending(c => c.CreatedAtUtc)
            .Select(c => new CandidateRow(
                c.Id,
                c.CandidateCode,
                c.Name,
                c.MobileNumber,
                c.Email,
                c.AppliedJobPosition!.Title,
                c.Concern!.Name,
                c.CurrentOrganization,
                c.CurrentDesignation,
                c.TotalYearsOfExperience,
                c.ExpectedSalary,
                c.Status,
                c.CreatedAtUtc,
                c.Documents.Any(d => d.Category == DocumentCategory.Cv && d.IsCurrent),
                c.Documents.Where(d => d.Category == DocumentCategory.Cv && d.IsCurrent)
                    .OrderByDescending(d => d.UploadedAtUtc)
                    .Select(d => d.FileName)
                    .FirstOrDefault(),
                c.Evaluations.Count,
                c.Remarks))
            .ToListAsync();
    }

    private IQueryable<Candidate> ApplyCandidateFilters(IQueryable<Candidate> rows, ReportQuery query)
    {
        if (query.DateFrom is { } from)
        {
            rows = rows.Where(c => c.CreatedAtUtc >= from.ToDateTime(new TimeOnly()));
        }

        if (query.DateTo is { } to)
        {
            rows = rows.Where(c => c.CreatedAtUtc < to.ToDateTime(new TimeOnly()).AddDays(1));
        }

        if (query.JobPositionId is { } jobId)
        {
            rows = rows.Where(c => c.AppliedJobPositionId == jobId);
        }

        if (query.ConcernId is { } concernId)
        {
            rows = rows.Where(c => c.ConcernId == concernId);
        }

        if (query.Status is { } status)
        {
            rows = rows.Where(c => c.Status == status);
        }

        var search = Trim(query.Search);
        if (search is not null)
        {
            var like = search.ToLower();
            rows = rows.Where(c =>
                c.Name.ToLower().Contains(like) ||
                c.CandidateCode.ToLower().Contains(like) ||
                (c.MobileNumber != null && c.MobileNumber.ToLower().Contains(like)) ||
                (c.CurrentOrganization != null && c.CurrentOrganization.ToLower().Contains(like)) ||
                (c.CurrentDesignation != null && c.CurrentDesignation.ToLower().Contains(like)));
        }

        return rows;
    }

    private async Task<List<InterviewRow>> LoadInterviewsAsync(ReportQuery query)
    {
        var rows = db.InterviewEvaluations.AsNoTracking().AsQueryable();

        if (query.DateFrom is { } from)
        {
            rows = rows.Where(e => e.InterviewDate >= from.ToDateTime(new TimeOnly()));
        }

        if (query.DateTo is { } to)
        {
            rows = rows.Where(e => e.InterviewDate < to.ToDateTime(new TimeOnly()).AddDays(1));
        }

        if (query.JobPositionId is { } jobId)
        {
            rows = rows.Where(e => e.AppliedJobPositionId == jobId ||
                                   (e.AppliedJobPositionId == null && e.Candidate.AppliedJobPositionId == jobId));
        }

        if (query.ConcernId is { } concernId)
        {
            rows = rows.Where(e => e.ConcernId == concernId ||
                                   (e.ConcernId == null && e.Candidate.ConcernId == concernId));
        }

        var search = Trim(query.Search);
        if (search is not null)
        {
            var like = search.ToLower();
            rows = rows.Where(e =>
                e.Candidate.Name.ToLower().Contains(like) ||
                e.Candidate.CandidateCode.ToLower().Contains(like));
        }

        return await rows
            .OrderByDescending(e => e.InterviewDate)
            .Select(e => new InterviewRow(
                e.Id,
                e.Candidate.CandidateCode,
                e.Candidate.Name,
                e.InterviewNumber,
                e.InterviewDate,
                e.AppliedJobPosition != null ? e.AppliedJobPosition.Title : e.Candidate.AppliedJobPosition!.Title,
                e.Status,
                e.Result != null ? e.Result.Decision : InterviewDecision.NotTaken,
                e.Result != null ? e.Result.Recommendation : null,
                e.Ratings.Sum(r => r.Rating),
                e.CreatedBy!.FullName))
            .ToListAsync();
    }

    private async Task<string> FilterSummaryAsync(ReportDefinition definition, ReportQuery query)
    {
        var parts = new List<string>();

        if (definition.Filters.Contains(ReportFilter.DateRange))
        {
            parts.Add(query.DateFrom is { } from && query.DateTo is { } to
                ? $"From {Label(from)} to {Label(to)}"
                : query.DateFrom is { } oneFrom
                    ? $"From {Label(oneFrom)}"
                    : query.DateTo is { } oneTo
                        ? $"Up to {Label(oneTo)}"
                        : "All dates");
        }

        if (definition.Filters.Contains(ReportFilter.Search) && Trim(query.Search) is { } search)
        {
            parts.Add($"Search: '{search}'");
        }

        if (definition.Filters.Contains(ReportFilter.JobPosition) && query.JobPositionId is { } jobId)
        {
            var title = await db.JobPositions.Where(p => p.Id == jobId).Select(p => p.Title).FirstOrDefaultAsync();
            parts.Add($"Position: {title ?? "#{jobId}"}");
        }

        if (definition.Filters.Contains(ReportFilter.Concern) && query.ConcernId is { } concernId)
        {
            var name = await db.Concerns.Where(c => c.Id == concernId).Select(c => c.Name).FirstOrDefaultAsync();
            parts.Add($"Concern: {name ?? "#{concernId}"}");
        }

        if (definition.Filters.Contains(ReportFilter.Status) && query.Status is { } status)
        {
            parts.Add($"Status: {Reports.StatusLabel(status)}");
        }

        if (definition.Filters.Contains(ReportFilter.CvOnFile) && (query.HasCvOnly || query.HasNoCv))
        {
            parts.Add(query.HasCvOnly ? "CV on file: yes" : "CV on file: no");
        }

        if (definition.Filters.Contains(ReportFilter.GroupBy))
        {
            parts.Add($"Grouped by: {ReportOptions.DimensionLabel(query.NormalizedGroupBy)}");
        }

        if (definition.Filters.Contains(ReportFilter.InterviewView) && query.InterviewView is not null &&
            string.Equals(query.InterviewView, "date", StringComparison.OrdinalIgnoreCase))
        {
            parts.Add("View: grouped by interview date");
        }

        return parts.Count == 0 ? "No filters applied" : string.Join("  ·  ", parts);
    }

    private static decimal? AverageYears(IEnumerable<CandidateRow> group)
    {
        var years = group.Select(c => c.YearsOfExperience).Where(v => v is not null).Select(v => v!.Value).ToList();
        return years.Count == 0 ? null : Math.Round(years.Average(), 1);
    }

    private static string? Trim(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string Day(DateTime value) => DateOnly.FromDateTime(value).ToString("yyyy-MM-dd");

    private static string Label(DateOnly value) => value.ToString("dd MMM yyyy");

    private static string InterviewStatusLabel(InterviewStatus status) => status switch
    {
        InterviewStatus.Pending => "Pending",
        InterviewStatus.Completed => "Completed",
        InterviewStatus.Cancelled => "Cancelled",
        _ => status.ToString()
    };

    private static string DecisionLabel(InterviewDecision decision) => decision switch
    {
        InterviewDecision.NotTaken => "Not taken",
        InterviewDecision.Selected => "Selected",
        InterviewDecision.Consider => "Consider",
        InterviewDecision.Reinterview => "Reinterview",
        InterviewDecision.Reject => "Rejected",
        _ => decision.ToString()
    };

    private sealed record CandidateRow(
        int Id,
        string Code,
        string Name,
        string? Mobile,
        string? Email,
        string? Position,
        string? Concern,
        string? Organization,
        string? Designation,
        decimal? YearsOfExperience,
        decimal? ExpectedSalary,
        CandidateStatus Status,
        DateTime RegisteredOn,
        bool HasCv,
        string? CvFileName,
        int Interviews,
        string? Remarks);

    private sealed record InterviewRow(
        int Id,
        string Code,
        string Name,
        int InterviewNumber,
        DateTime InterviewDate,
        string? Position,
        InterviewStatus Status,
        InterviewDecision Decision,
        string? Recommendation,
        int TotalRating,
        string? CreatedByName);
}
