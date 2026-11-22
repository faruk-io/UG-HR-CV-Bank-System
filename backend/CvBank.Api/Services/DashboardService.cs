using CvBank.Api.Data;
using CvBank.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CvBank.Api.Services;

public class DashboardService(CvBankDbContext db)
{
    public async Task<DashboardSummaryDto> SummaryAsync()
    {
        var candidates = await db.Candidates.AsNoTracking()
            .Include(c => c.AppliedJobPosition)
            .Include(c => c.Documents.Where(d => d.Category == DocumentCategory.Cv && d.IsCurrent))
            .ToListAsync();

        var evaluations = await db.InterviewEvaluations.AsNoTracking()
            .Include(e => e.Result)
            .Select(e => new
            {
                e.Id,
                e.CandidateId,
                e.InterviewNumber,
                e.InterviewDate,
                e.Status,
                Decision = e.Result == null ? InterviewDecision.NotTaken : e.Result.Decision,
                CandidateCode = e.Candidate.CandidateCode,
                CandidateName = e.Candidate.Name
            })
            .ToListAsync();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var joiningTotal = await db.JoiningRecords.CountAsync();
        var recentJoining = await db.JoiningRecords.AsNoTracking()
            .OrderByDescending(x => x.Id)
            .Take(6)
            .Select(x => new RecentJoiningDto(x.Id, x.EmployeeId, x.Name, x.Designation, x.DateOfJoining))
            .ToListAsync();

        var resignedTotal = await db.ResignedRecords.CountAsync();
        var recentResigned = await db.ResignedRecords.AsNoTracking()
            .OrderByDescending(x => x.Id)
            .Take(6)
            .Select(x => new RecentResignedDto(x.Id, x.EmployeeId, x.Name, x.ResignDate))
            .ToListAsync();

        var byDate = evaluations
            .GroupBy(e => DateOnly.FromDateTime(e.InterviewDate))
            .Where(g => g.Key <= today)
            .OrderByDescending(g => g.Key)
            .Take(14)
            .Select(g => new DateWiseInterviewDto(
                g.Key,
                g.Count(),
                g.Count(x => x.Decision == InterviewDecision.Selected)))
            .ToList();

        return new DashboardSummaryDto(
            TotalCandidates: candidates.Count,
            TotalCvUploaded: candidates.Count(c =>
                c.Documents.Any(d => d.Category == DocumentCategory.Cv && d.IsCurrent)),
            TotalInterviews: evaluations.Count,
            Selected: candidates.Count(c => c.Status == CandidateStatus.Selected),
            RecentCandidates: candidates
                .OrderByDescending(c => c.CreatedAtUtc)
                .Take(8)
                .Select(c => new RecentCandidateDto(
                    c.Id,
                    c.CandidateCode,
                    c.Name,
                    c.MobileNumber,
                    c.AppliedJobPosition?.Title,
                    c.Status,
                    c.Documents.Any(d => d.Category == DocumentCategory.Cv && d.IsCurrent),
                    c.CreatedAtUtc))
                .ToList(),
            RecentInterviews: evaluations
                .OrderByDescending(e => e.InterviewDate)
                .ThenByDescending(e => e.Id)
                .Take(8)
                .Select(e => new RecentInterviewDto(
                    e.Id, e.CandidateId, e.CandidateCode, e.CandidateName,
                    e.InterviewNumber, e.InterviewDate, e.Status, e.Decision))
                .ToList(),
            InterviewByDate: byDate,
            TotalJoining: joiningTotal,
            RecentJoining: recentJoining,
            TotalResigned: resignedTotal,
            RecentResigned: recentResigned);
    }

    public async Task<CandidateSummaryDto> CandidateSummaryAsync(int candidateId)
    {
        var candidate = await db.Candidates.AsNoTracking()
            .Include(c => c.AppliedJobPosition)
            .Include(c => c.Concern)
            .Include(c => c.Documents.Where(d => d.IsCurrent).OrderByDescending(d => d.UploadedAtUtc))
            .Include(c => c.Evaluations.OrderByDescending(e => e.InterviewNumber).Take(1))
                .ThenInclude(e => e.Ratings)
            .Include(c => c.Evaluations.OrderByDescending(e => e.InterviewNumber).Take(1))
                .ThenInclude(e => e.Result!.DesignationFixed)
            .Include(c => c.Evaluations.OrderByDescending(e => e.InterviewNumber).Take(1))
                .ThenInclude(e => e.Evaluators)
            .FirstOrDefaultAsync(c => c.Id == candidateId)
            ?? throw new NotFoundException($"Candidate {candidateId} was not found.");

        var evaluation = candidate.Evaluations.OrderByDescending(e => e.InterviewNumber).FirstOrDefault();

        var ratings = evaluation is null
            ? []
            : evaluation.Ratings
                .OrderBy(r => r.DimensionCode)
                .Select(r => new RatingLineDto(r.DimensionName, r.Rating, 5))
                .ToList();

        var totalRating = evaluation?.Ratings.Sum(r => r.Rating) ?? 0;

        var findings = evaluation is null
            ? null
            : string.Join(" | ", evaluation.Evaluators
                .Where(e => !string.IsNullOrWhiteSpace(e.Findings))
                .OrderBy(e => e.SectionNumber)
                .Select(e => e.Findings!.Trim()));

        var documents = candidate.Documents
            .Select(d => new SummaryDocument(d.Id, d.FileName, d.ContentType, d.SizeBytes, d.UploadedAtUtc))
            .ToList();

        return new CandidateSummaryDto(
            candidate.Id,
            candidate.CandidateCode,
            DateTime.UtcNow,
            new SummaryCandidateInfo(
                candidate.Name,
                candidate.MobileNumber,
                candidate.Email,
                candidate.DateOfBirth,
                candidate.PresentAddress,
                candidate.Remarks),
            new SummaryJobInterview(
                candidate.AppliedJobPosition?.Title,
                candidate.Concern?.Name,
                evaluation?.InterviewDate,
                evaluation?.InterviewNumber,
                candidate.Status.ToString(),
                evaluation?.Result == null ? null : evaluation.Result.Decision.ToString()),
            new SummaryEmployment(
                candidate.CurrentOrganization,
                candidate.CurrentDesignation,
                candidate.DurationInCurrentOrganization,
                candidate.TotalYearsOfExperience,
                candidate.PresentSalary,
                candidate.ExpectedSalary,
                candidate.NoticePeriod),
            evaluation is null
                ? null
                : new SummaryEvaluation(
                    evaluation.InterviewNumber,
                    evaluation.InterviewDate,
                    totalRating,
                    EvaluationTemplate.MaxTotalRating,
                    ratings,
                    string.IsNullOrWhiteSpace(findings) ? null : findings,
                    evaluation.Result?.Recommendation,
                    evaluation.Result?.Decision.ToString() ?? InterviewDecision.NotTaken.ToString(),
                    evaluation.Result?.DesignationFixed?.Name,
                    evaluation.Result?.SalaryFixed,
                    evaluation.Result?.Grade,
                    evaluation.Result?.DateOfJoining),
            documents.Where(d => IsCv(candidate, d.DocumentId)).ToList(),
            documents.Where(d => !IsCv(candidate, d.DocumentId)).ToList());
    }

    private static bool IsCv(Candidate candidate, int documentId) =>
        candidate.Documents.Any(d => d.Id == documentId && d.Category == DocumentCategory.Cv);
}
