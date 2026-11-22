using System.ComponentModel.DataAnnotations;

namespace CvBank.Api.Models;

public class EmployeeListQuery
{
    public string? Search { get; set; }
    public int? ConcernId { get; set; }
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public sealed record JoiningRecordDto(
    int Id,
    string EmployeeId,
    string Name,
    string? Designation,
    string? Department,
    decimal? Salary,
    int? ConcernId,
    string? Concern,
    DateTime? DateOfJoining,
    string? ProbationPeriod,
    decimal? AfterProbationSalary,
    string? EffectiveSalary,
    //decimal? EffectiveSalary,
    string? Remarks,
    DateTime CreatedAtUtc,
    string? CreatedByName,
    DateTime? UpdatedAtUtc);

public sealed record ResignedRecordDto(
    int Id,
    string EmployeeId,
    string Name,
    string? Designation,
    string? Department,
    decimal? Salary,
    int? ConcernId,
    string? Concern,
    DateTime? DateOfJoining,
    DateTime? ResignDate,
    DateTime? LastWorkingDay,
    DateTime? EffectiveFrom,
    string? NoticePeriod,
    bool HrApproved,
    string? CauseOfResignation,
    string? Remarks,
    DateTime CreatedAtUtc,
    string? CreatedByName,
    DateTime? UpdatedAtUtc);

public class JoiningSaveRequest
{
    [ StringLength(50)]
    public string? EmployeeId { get; set; }

    [Required, StringLength(200)]
    public string Name { get; set; } = null!;

    [StringLength(200)]
    public string? Designation { get; set; }

    [StringLength(200)]
    public string? Department { get; set; }

    [Range(0, 100_000_000)]
    public decimal? Salary { get; set; }

    public int? ConcernId { get; set; }

    public DateTime? DateOfJoining { get; set; }

    [StringLength(100)]
    public string? ProbationPeriod { get; set; }

    [Range(0, 100_000_000)]
    public decimal? AfterProbationSalary { get; set; }

    [StringLength(50)]
    public string? EffectiveSalary { get; set; }

    //[Range(0, 100_000_000)]
    //public decimal? EffectiveSalary { get; set; }

    [StringLength(1000)]
    public string? Remarks { get; set; }
}

public class ResignedSaveRequest
{
    [Required, StringLength(50)]
    public string EmployeeId { get; set; } = null!;

    [Required, StringLength(200)]
    public string Name { get; set; } = null!;

    [StringLength(200)]
    public string? Designation { get; set; }

    [StringLength(200)]
    public string? Department { get; set; }

    [Range(0, 100_000_000)]
    public decimal? Salary { get; set; }

    public int? ConcernId { get; set; }

    public DateTime? DateOfJoining { get; set; }

    public DateTime? ResignDate { get; set; }

    public DateTime? LastWorkingDay { get; set; }

    public DateTime? EffectiveFrom { get; set; }

    [StringLength(100)]
    public string? NoticePeriod { get; set; }

    public bool HrApproved { get; set; }

    [StringLength(500)]
    public string? CauseOfResignation { get; set; }

    [StringLength(1000)]
    public string? Remarks { get; set; }
}