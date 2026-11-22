using System.ComponentModel.DataAnnotations;

namespace CvBank.Api.Models;

public class CandidateQuery
{
    public string? Search { get; set; }
    public CandidateStatus? Status { get; set; }
    public int? JobPositionId { get; set; }
    public int? ConcernId { get; set; }
    public decimal? MinExperience { get; set; }
    public decimal? MaxExperience { get; set; }
    public bool? HasCv { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public sealed record CandidateListItemDto(
    int Id,
    string CandidateCode,
    string Name,
    string? MobileNumber,
    string? Email,
    string? CurrentOrganization,
    string? CurrentDesignation,
    string? AppliedPosition,
    string? Concern,
    decimal? TotalYearsOfExperience,
    CandidateStatus Status,
    bool HasCv,
    DateTime CreatedAtUtc);

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);

public sealed record CandidateDocumentDto(
    int Id,
    DocumentCategory Category,
    string FileName,
    string ContentType,
    long SizeBytes,
    bool IsCurrent,
    DateTime UploadedAtUtc,
    string? UploadedByName,
    string? Remarks);

public sealed record CandidateDto(
    int Id,
    string CandidateCode,
    string Name,
    string? MobileNumber,
    string? Email,
    DateTime? DateOfBirth,
    string? PresentAddress,
    string? CurrentOrganization,
    string? CurrentDesignation,
    string? DurationInCurrentOrganization,
    decimal? TotalYearsOfExperience,
    int? AppliedJobPositionId,
    string? AppliedPosition,
    int? ConcernId,
    string? Concern,
    decimal? PresentSalary,
    decimal? ExpectedSalary,
    string? NoticePeriod,
    string? Remarks,
    CandidateStatus Status,
    DateTime CreatedAtUtc,
    string? CreatedByName,
    DateTime? UpdatedAtUtc,
    int InterviewCount,
    IReadOnlyList<CandidateDocumentDto> Documents);

public class CandidateSaveRequest
{
    [Required, StringLength(200)]
    public string Name { get; set; } = null!;

    [StringLength(30)]
    public string? MobileNumber { get; set; }

    [EmailAddress, StringLength(200)]
    public string? Email { get; set; }

    public DateTime? DateOfBirth { get; set; }

    [StringLength(500)]
    public string? PresentAddress { get; set; }

    [StringLength(200)]
    public string? CurrentOrganization { get; set; }

    [StringLength(200)]
    public string? CurrentDesignation { get; set; }

    [StringLength(100)]
    public string? DurationInCurrentOrganization { get; set; }

    [Range(0, 60)]
    public decimal? TotalYearsOfExperience { get; set; }

    public int? AppliedJobPositionId { get; set; }

    public int? ConcernId { get; set; }

    [Range(0, 100_000_000)]
    public decimal? PresentSalary { get; set; }

    [Range(0, 100_000_000)]
    public decimal? ExpectedSalary { get; set; }

    [StringLength(100)]
    public string? NoticePeriod { get; set; }

    [StringLength(1000)]
    public string? Remarks { get; set; }

    public CandidateStatus? Status { get; set; }

    public bool AcceptPossibleDuplicate { get; set; }
}

public sealed record DuplicateCandidateDto(
    int Id,
    string CandidateCode,
    string Name,
    string? MobileNumber,
    string? Email,
    CandidateStatus Status,
    string MatchedOn);

public sealed record CandidateStatusRequest(CandidateStatus Status);

public sealed record MasterDataOption(int Id, string Name);

public class MasterDataCreateRequest
{
    [Required, StringLength(200)]
    public string Name { get; set; } = null!;
}
