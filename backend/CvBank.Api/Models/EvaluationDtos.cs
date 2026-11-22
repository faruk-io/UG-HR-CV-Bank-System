using System.ComponentModel.DataAnnotations;

namespace CvBank.Api.Models;

public class EvaluationQuery
{
    public string? Search { get; set; }
    public int? CandidateId { get; set; }
    public int? JobPositionId { get; set; }
    public InterviewStatus? Status { get; set; }
    public InterviewDecision? Decision { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public bool? HasScannedForm { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public sealed record EvaluationListItemDto(
    int Id,
    int CandidateId,
    string CandidateCode,
    string CandidateName,
    string? AppliedPosition,
    string? Concern,
    int InterviewNumber,
    DateTime InterviewDate,
    InterviewStatus Status,
    InterviewDecision Decision,
    int TotalRating,
    int MaxRating,
    bool HasScannedForm,
    DateTime CreatedAtUtc,
    string? CreatedByName);

public sealed record CandidateEvaluationHistoryItemDto(
    int Id,
    int InterviewNumber,
    DateTime InterviewDate,
    InterviewStatus Status,
    string? AppliedPosition,
    string? Concern,
    int TotalRating,
    int MaxRating,
    InterviewDecision Decision,
    string? Recommendation,
    string? EvaluatorNames,
    bool HasScannedForm,
    string? FixedDesignation,
    decimal? FixedSalary,
    string? Grade,
    DateTime? DateOfJoining,
    string? CreatedByName);

public sealed record EvaluationRatingLineDto(int DimensionCode, string DimensionName, int Rating);

public sealed record DocumentItemLineDto(
    int ItemCode,
    string ItemName,
    string? Details,
    bool CopySubmitted,
    bool OriginalChecked);

public sealed record ReferenceLineDto(
    int ReferenceNumber,
    string? ReferenceName,
    string? Designation,
    string? Relationship,
    string? CompanyAndLocation,
    string? Phone,
    string? Statement);

public sealed record EvaluatorLineDto(
    int SectionNumber,
    string? Findings,
    string? EvaluatorName,
    string? Designation,
    string? Department,
    string? SignatureText);

public sealed record ApprovalLineDto(
    int AuthorityNumber,
    string AuthorityTitle,
    string? Decision,
    string? SignatureText,
    DateTime? ActedAtUtc);

public sealed record ResultLineDto(
    InterviewDecision Decision,
    string? Recommendation,
    decimal? ExpectedSalary,
    string? NoticePeriod,
    int? DesignationFixedId,
    string? DesignationFixed,
    decimal? SalaryFixed,
    string? Grade,
    DateTime? DateOfJoining,
    string? Remarks);

public sealed record EvaluationFormCandidateDto(
    int CandidateId,
    string CandidateCode,
    string Name,
    string? MobileNumber,
    string? Email,
    DateTime? DateOfBirth,
    string? CurrentOrganization,
    string? CurrentDesignation,
    decimal? PresentSalary,
    string? DurationInCurrentOrganization,
    decimal? TotalYearsOfExperience,
    string? PresentAddress,
    decimal? ExpectedSalary,
    string? NoticePeriod);

public sealed record EvaluationDto(
    int Id,
    int CandidateId,
    string CandidateCode,
    int InterviewNumber,
    DateTime InterviewDate,
    InterviewStatus Status,
    int? AppliedJobPositionId,
    string? AppliedPosition,
    int? ConcernId,
    string? Concern,
    int? PersonsInterviewTaken,
    string? Remarks,
    int TotalRating,
    int MaxRating,
    string RatingScaleText,
    EvaluationFormCandidateDto Candidate,
    IReadOnlyList<EvaluationRatingLineDto> Ratings,
    IReadOnlyList<DocumentItemLineDto> DocumentItems,
    IReadOnlyList<ReferenceLineDto> References,
    IReadOnlyList<EvaluatorLineDto> Evaluators,
    ResultLineDto? Result,
    IReadOnlyList<ApprovalLineDto> Approvals,
    IReadOnlyList<CandidateDocumentDto> ScannedForms,
    DateTime CreatedAtUtc,
    int? CreatedById,
    string? CreatedByName);

public class RatingInput
{
    [Range(1, 10)] public int DimensionCode { get; set; }

    [Range(0, 5)] public int Rating { get; set; }
}

public class DocumentItemInput
{
    [Range(1, 8)] public int ItemCode { get; set; }

    [StringLength(500)] public string? Details { get; set; }

    public bool CopySubmitted { get; set; }

    public bool OriginalChecked { get; set; }
}

public class ReferenceInput
{
    [Range(1, 2)] public int ReferenceNumber { get; set; }

    [StringLength(200)] public string? ReferenceName { get; set; }

    [StringLength(200)] public string? Designation { get; set; }

    [StringLength(100)] public string? Relationship { get; set; }

    [StringLength(300)] public string? CompanyAndLocation { get; set; }

    [StringLength(50)] public string? Phone { get; set; }

    [StringLength(2000)] public string? Statement { get; set; }
}

public class EvaluatorInput
{
    [Range(1, 4)] public int SectionNumber { get; set; }

    [StringLength(2000)] public string? Findings { get; set; }

    [StringLength(200)] public string? EvaluatorName { get; set; }

    [StringLength(200)] public string? Designation { get; set; }

    [StringLength(200)] public string? Department { get; set; }

    [StringLength(200)] public string? SignatureText { get; set; }
}

public class ApprovalInput
{
    [Range(1, 5)] public int AuthorityNumber { get; set; }

    [StringLength(50)] public string? Decision { get; set; }

    [StringLength(200)] public string? SignatureText { get; set; }

    public DateTime? ActedAtUtc { get; set; }
}

public class ResultInput
{
    public InterviewDecision Decision { get; set; } = InterviewDecision.NotTaken;

    [StringLength(1000)] public string? Recommendation { get; set; }

    [Range(0, 100000000)] public decimal? ExpectedSalary { get; set; }

    [StringLength(100)] public string? NoticePeriod { get; set; }

    public int? DesignationFixedId { get; set; }

    [Range(0, 100000000)] public decimal? SalaryFixed { get; set; }

    [StringLength(50)] public string? Grade { get; set; }

    public DateTime? DateOfJoining { get; set; }

    [StringLength(1000)] public string? Remarks { get; set; }
}

public class EvaluationSaveRequest
{
    [Required] public int CandidateId { get; set; }

    public int? InterviewNumber { get; set; }

    [Required] public DateTime InterviewDate { get; set; }

    public InterviewStatus Status { get; set; } = InterviewStatus.Pending;

    public int? AppliedJobPositionId { get; set; }

    public int? ConcernId { get; set; }

    [Range(0, 99)] public int? PersonsInterviewTaken { get; set; }

    [StringLength(1000)] public string? Remarks { get; set; }

    public List<RatingInput> Ratings { get; set; } = [];

    public List<DocumentItemInput> DocumentItems { get; set; } = [];

    public List<ReferenceInput> References { get; set; } = [];

    public List<EvaluatorInput> Evaluators { get; set; } = [];

    public ResultInput? Result { get; set; }

    public List<ApprovalInput> Approvals { get; set; } = [];
}
