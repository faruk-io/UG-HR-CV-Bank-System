namespace CvBank.Api.Models;

public enum CandidateStatus
{
    Shortlisted = 1,
    Selected = 2,
    OptionalBackup = 3,
    OverBudget = 4,
    Fresher = 5
}

public enum DocumentCategory
{
    Cv = 1,
    CompletedEvaluationForm = 2,
    Other = 3
}

public class Candidate
{
    public int Id { get; set; }
    public string CandidateCode { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? MobileNumber { get; set; }
    public string? Email { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? PresentAddress { get; set; }
    public string? CurrentOrganization { get; set; }
    public string? CurrentDesignation { get; set; }
    public string? DurationInCurrentOrganization { get; set; }
    public decimal? TotalYearsOfExperience { get; set; }
    public int? AppliedJobPositionId { get; set; }
    public JobPosition? AppliedJobPosition { get; set; }
    public int? ConcernId { get; set; }
    public Concern? Concern { get; set; }
    public decimal? PresentSalary { get; set; }
    public decimal? ExpectedSalary { get; set; }
    public string? NoticePeriod { get; set; }
    public string? Remarks { get; set; }
    public CandidateStatus Status { get; set; } = CandidateStatus.Shortlisted;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public int? CreatedById { get; set; }
    public User? CreatedBy { get; set; }

    public ICollection<CandidateDocument> Documents { get; set; } = new List<CandidateDocument>();
    public ICollection<InterviewEvaluation> Evaluations { get; set; } = new List<InterviewEvaluation>();
}

public class CandidateDocument
{
    public int Id { get; set; }
    public int CandidateId { get; set; }
    public Candidate Candidate { get; set; } = null!;
    public int? InterviewEvaluationId { get; set; }
    public InterviewEvaluation? InterviewEvaluation { get; set; }
    public DocumentCategory Category { get; set; }
    public string FileName { get; set; } = null!;
    public string StoredFileName { get; set; } = null!;
    public string ContentType { get; set; } = null!;
    public long SizeBytes { get; set; }
    public bool IsCurrent { get; set; } = true;
    public string? Remarks { get; set; }
    public DateTime UploadedAtUtc { get; set; }
    public int? UploadedById { get; set; }
    public User? UploadedBy { get; set; }
}
