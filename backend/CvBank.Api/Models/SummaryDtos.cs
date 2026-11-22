namespace CvBank.Api.Models;

public sealed record DashboardSummaryDto(
    int TotalCandidates,
    int TotalCvUploaded,
    int TotalInterviews,
    int Selected,
    IReadOnlyList<RecentCandidateDto> RecentCandidates,
    IReadOnlyList<RecentInterviewDto> RecentInterviews,
    IReadOnlyList<DateWiseInterviewDto> InterviewByDate,
    int TotalJoining,
    IReadOnlyList<RecentJoiningDto> RecentJoining,
    int TotalResigned,
    IReadOnlyList<RecentResignedDto> RecentResigned);

public sealed record RecentJoiningDto(
    int Id,
    string EmployeeId,
    string Name,
    string? Designation,
    DateTime? DateOfJoining);

public sealed record RecentResignedDto(
    int Id,
    string EmployeeId,
    string Name,
    DateTime? ResignDate);

public sealed record RecentCandidateDto(
    int Id,
    string CandidateCode,
    string Name,
    string? MobileNumber,
    string? AppliedPosition,
    CandidateStatus Status,
    bool HasCv,
    DateTime CreatedAtUtc);

public sealed record RecentInterviewDto(
    int Id,
    int CandidateId,
    string CandidateCode,
    string CandidateName,
    int InterviewNumber,
    DateTime InterviewDate,
    InterviewStatus Status,
    InterviewDecision Decision);

public sealed record DateWiseInterviewDto(DateOnly Date, int Total, int Selected);

public sealed record SummaryCandidateInfo(
    string Name,
    string? MobileNumber,
    string? Email,
    DateTime? DateOfBirth,
    string? PresentAddress,
    string? Remarks);

public sealed record SummaryJobInterview(
    string? AppliedPosition,
    string? Concern,
    DateTime? InterviewDate,
    int? InterviewNumber,
    string InterviewStatus,
    string? FinalDecision);

public sealed record SummaryEmployment(
    string? CurrentOrganization,
    string? CurrentDesignation,
    string? DurationInCurrentOrganization,
    decimal? TotalYearsOfExperience,
    decimal? PresentSalary,
    decimal? ExpectedSalary,
    string? NoticePeriod);

public sealed record SummaryEvaluation(
    int InterviewNumber,
    DateTime InterviewDate,
    int TotalRating,
    int MaxRating,
    IReadOnlyList<RatingLineDto> Ratings,
    string? EvaluatorFindings,
    string? Recommendation,
    string Decision,
    string? DesignationFixed,
    decimal? SalaryFixed,
    string? Grade,
    DateTime? DateOfJoining);

public sealed record RatingLineDto(string Dimension, int Rating, int MaxRating);

public sealed record SummaryDocument(
    int DocumentId,
    string FileName,
    string ContentType,
    long SizeBytes,
    DateTime UploadedAtUtc);

public sealed record CandidateSummaryDto(
    int CandidateId,
    string CandidateCode,
    DateTime GeneratedAtUtc,
    SummaryCandidateInfo Candidate,
    SummaryJobInterview JobInterview,
    SummaryEmployment Employment,
    SummaryEvaluation? Evaluation,
    IReadOnlyList<SummaryDocument> CvDocuments,
    IReadOnlyList<SummaryDocument> OtherDocuments);
