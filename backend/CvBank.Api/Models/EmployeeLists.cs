namespace CvBank.Api.Models;

public class JoiningRecord
{
    public int Id { get; set; }
    public string EmployeeId { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Designation { get; set; }
    public string? Department { get; set; }
    public decimal? Salary { get; set; }
    public int? ConcernId { get; set; }
    public Concern? Concern { get; set; }
    public DateTime? DateOfJoining { get; set; }
    public string? ProbationPeriod { get; set; }
    public decimal? AfterProbationSalary { get; set; }
    public string? EffectiveSalary { get; set; }
    //public decimal? EffectiveSalary { get; set; }
    public string? Remarks { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public int? CreatedById { get; set; }
    public User? CreatedBy { get; set; }
}

public class ResignedRecord
{
    public int Id { get; set; }
    public string EmployeeId { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Designation { get; set; }
    public string? Department { get; set; }
    public decimal? Salary { get; set; }
    public int? ConcernId { get; set; }
    public Concern? Concern { get; set; }
    public DateTime? DateOfJoining { get; set; }
    public DateTime? ResignDate { get; set; }
    public DateTime? LastWorkingDay { get; set; }
    public DateTime? EffectiveFrom { get; set; }
    public string? NoticePeriod { get; set; }
    public bool HrApproved { get; set; }
    public string? CauseOfResignation { get; set; }
    public string? Remarks { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public int? CreatedById { get; set; }
    public User? CreatedBy { get; set; }
}