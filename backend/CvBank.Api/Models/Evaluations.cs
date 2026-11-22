namespace CvBank.Api.Models;

public enum InterviewStatus
{
    Pending = 1,
    Completed = 2,
    Cancelled = 3
}

public enum InterviewDecision
{
    NotTaken = 0,
    Selected = 1,
    Consider = 2,
    Reinterview = 3,
    Reject = 4
}

public class InterviewEvaluation
{
    public int Id { get; set; }
    public int CandidateId { get; set; }
    public Candidate Candidate { get; set; } = null!;
    public int InterviewNumber { get; set; }
    public DateTime InterviewDate { get; set; }
    public InterviewStatus Status { get; set; } = InterviewStatus.Pending;
    public int? AppliedJobPositionId { get; set; }
    public JobPosition? AppliedJobPosition { get; set; }
    public int? ConcernId { get; set; }
    public Concern? Concern { get; set; }
    public int? PersonsInterviewTaken { get; set; }
    public string? Remarks { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public int? CreatedById { get; set; }
    public User? CreatedBy { get; set; }

    public ICollection<EvaluationRating> Ratings { get; set; } = new List<EvaluationRating>();
    public ICollection<EvaluationDocumentItem> DocumentItems { get; set; } = new List<EvaluationDocumentItem>();
    public ICollection<InterviewReference> References { get; set; } = new List<InterviewReference>();
    public ICollection<EvaluatorDetail> Evaluators { get; set; } = new List<EvaluatorDetail>();
    public ICollection<InterviewApproval> Approvals { get; set; } = new List<InterviewApproval>();
    public InterviewResult? Result { get; set; }
}

public class EvaluationRating
{
    public int Id { get; set; }
    public int InterviewEvaluationId { get; set; }
    public InterviewEvaluation InterviewEvaluation { get; set; } = null!;
    public int DimensionCode { get; set; }
    public string DimensionName { get; set; } = null!;
    public int Rating { get; set; }
}

public class EvaluationDocumentItem
{
    public int Id { get; set; }
    public int InterviewEvaluationId { get; set; }
    public InterviewEvaluation InterviewEvaluation { get; set; } = null!;
    public int ItemCode { get; set; }
    public string ItemName { get; set; } = null!;
    public string? Details { get; set; }
    public bool CopySubmitted { get; set; }
    public bool OriginalChecked { get; set; }
}

public class InterviewReference
{
    public int Id { get; set; }
    public int InterviewEvaluationId { get; set; }
    public InterviewEvaluation InterviewEvaluation { get; set; } = null!;
    public int ReferenceNumber { get; set; }
    public string? ReferenceName { get; set; }
    public string? Designation { get; set; }
    public string? Relationship { get; set; }
    public string? CompanyAndLocation { get; set; }
    public string? Phone { get; set; }
    public string? Statement { get; set; }
}

public class EvaluatorDetail
{
    public int Id { get; set; }
    public int InterviewEvaluationId { get; set; }
    public InterviewEvaluation InterviewEvaluation { get; set; } = null!;
    public int SectionNumber { get; set; }
    public string? Findings { get; set; }
    public string? EvaluatorName { get; set; }
    public string? Designation { get; set; }
    public string? Department { get; set; }
    public string? SignatureText { get; set; }
}

public class InterviewResult
{
    public int Id { get; set; }
    public int InterviewEvaluationId { get; set; }
    public InterviewEvaluation InterviewEvaluation { get; set; } = null!;
    public InterviewDecision Decision { get; set; } = InterviewDecision.NotTaken;
    public string? Recommendation { get; set; }
    public decimal? ExpectedSalary { get; set; }
    public string? NoticePeriod { get; set; }
    public int? DesignationFixedId { get; set; }
    public Designation? DesignationFixed { get; set; }
    public decimal? SalaryFixed { get; set; }
    public string? Grade { get; set; }
    public DateTime? DateOfJoining { get; set; }
    public string? Remarks { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public class InterviewApproval
{
    public int Id { get; set; }
    public int InterviewEvaluationId { get; set; }
    public InterviewEvaluation InterviewEvaluation { get; set; } = null!;
    public int AuthorityNumber { get; set; }
    public string AuthorityTitle { get; set; } = null!;
    public string? Decision { get; set; }
    public string? SignatureText { get; set; }
    public DateTime? ActedAtUtc { get; set; }
}

public static class EvaluationTemplate
{
    public static readonly (int Code, string Name)[] Dimensions =
    [
        (1, "General Appearance"),
        (2, "Functional Knowledge"),
        (3, "Industry Experience"),
        (4, "Relevent Education"),
        (5, "Team & Organization Fit"),
        (6, "Communication Skill"),
        (7, "Physical & Psycological Fit"),
        (8, "Job Fit"),
        (9, "Problem Solving"),
        (10, "Overall Personality")
    ];

    public static readonly (int Code, string Name)[] DocumentChecklist =
    [
        (1, "Updated CV"),
        (2, "Recent Photograph"),
        (3, "Academic & Professional Certificates"),
        (4, "NID/Passport/Birth Certificate"),
        (5, "Release letter/NOC from last employer"),
        (6, "Experience certificates"),
        (7, "Major Medical Complexity if any"),
        (8, "Others (if any)")
    ];

    public const string RatingScaleText =
        "Rating Scale : 5 ( Outstanding ) / 4 (Strong) / 3 ( Capable) / 2 ( weak) / 1 ( very weak)";

    public static readonly string[] ApprovalAuthorities =
    [
        "Concerned HOD",
        "Business Head",
        "HOD - HR",
        "Director",
        "Managing Director"
    ];

    public const int EvaluatorSections = 4;
    public const int ReferenceSections = 2;
    public const int MaxTotalRating = 50;
}
