using CvBank.Api.Auth;
using CvBank.Api.Data;
using CvBank.Api.Models;
using CvBank.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CvBank.Api.Controllers;

[ApiController]
[Route("api/joining")]
[Authorize]
public class JoiningController(CvBankDbContext db, IAuditWriter audit) : ControllerBase
{
    private SignedInUser Me => SignedInUser.From(User);

    [HttpGet]
    public async Task<ActionResult<PagedResult<JoiningRecordDto>>> List([FromQuery] EmployeeListQuery query)
    {
        var rows = Filtered(query);

        var total = await rows.CountAsync();
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var page = Math.Max(1, query.Page);

        var items = await rows.OrderBy(x => x.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(Projection)
            .ToListAsync();

        return Ok(new PagedResult<JoiningRecordDto>(items, total, page, pageSize));
    }

    [HttpGet("export")]
    public async Task<ActionResult<ReportData>> Export([FromQuery] EmployeeListQuery query)
    {
        var report = await BuildReportAsync(query);

        await audit.WriteAsync(Me.UserId, Me.FullName, "Joining PDF Export", "JoiningRecord", null,
            $"{report.Rows.Count} rows · {report.FilterSummary}");

        return Ok(report);
    }

    [HttpGet("excel")]
    public async Task<IActionResult> Excel([FromQuery] EmployeeListQuery query)
    {
        var report = await BuildReportAsync(query);
        var bytes = ReportExcel.Write(report, Me.FullName);

        await audit.WriteAsync(Me.UserId, Me.FullName, "Joining Excel Export", "JoiningRecord", null,
            $"{ReportExcel.FileName(report)} · {report.Rows.Count} rows · {report.FilterSummary}");

        return File(bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ReportExcel.FileName(report));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<JoiningRecordDto>> Get(int id)
    {
        var dto = await db.JoiningRecords.Where(x => x.Id == id).Select(Projection).FirstOrDefaultAsync()
            ?? throw new NotFoundException("Joining entry not found.");

        return Ok(dto);
    }

    [HttpPost]
    public async Task<ActionResult<JoiningRecordDto>> Create(JoiningSaveRequest request)
    {
        var entity = new JoiningRecord
        {
            CreatedAtUtc = DateTime.UtcNow,
            CreatedById = Me.UserId
        };

        await ApplyAsync(entity, request);
        db.JoiningRecords.Add(entity);
        await db.SaveChangesAsync();

        await audit.WriteAsync(Me.UserId, Me.FullName, "Joining Create", "JoiningRecord",
            entity.Id.ToString(), $"{entity.EmployeeId} | {entity.Name}");

        return await Get(entity.Id);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<JoiningRecordDto>> Update(int id, JoiningSaveRequest request)
    {
        var entity = await db.JoiningRecords.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new NotFoundException("Joining entry not found.");

        await ApplyAsync(entity, request);
        entity.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();

        await audit.WriteAsync(Me.UserId, Me.FullName, "Joining Update", "JoiningRecord",
            entity.Id.ToString(), $"{entity.EmployeeId} | {entity.Name}");

        return await Get(entity.Id);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var entity = await db.JoiningRecords.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new NotFoundException("Joining entry not found.");

        db.JoiningRecords.Remove(entity);
        await db.SaveChangesAsync();

        await audit.WriteAsync(Me.UserId, Me.FullName, "Joining Delete", "JoiningRecord",
            id.ToString(), $"{entity.EmployeeId} | {entity.Name}");

        return NoContent();
    }

    private IQueryable<JoiningRecord> Filtered(EmployeeListQuery query)
    {
        var rows = db.JoiningRecords.AsQueryable();

        var search = query.Search?.Trim();
        if (!string.IsNullOrEmpty(search))
        {
            rows = rows.Where(x =>
                x.EmployeeId.Contains(search) ||
                x.Name.Contains(search) ||
                (x.Designation != null && x.Designation.Contains(search)) ||
                (x.Department != null && x.Department.Contains(search)) ||
                (x.Concern != null && x.Concern.Name.Contains(search)));
        }

        if (query.ConcernId.HasValue)
        {
            rows = rows.Where(x => x.ConcernId == query.ConcernId.Value);
        }

        if (query.DateFrom.HasValue)
        {
            var from = query.DateFrom.Value.ToDateTime(TimeOnly.MinValue);
            rows = rows.Where(x => x.DateOfJoining >= from);
        }

        if (query.DateTo.HasValue)
        {
            var toExclusive = query.DateTo.Value.ToDateTime(TimeOnly.MinValue).AddDays(1);
            rows = rows.Where(x => x.DateOfJoining < toExclusive);
        }

        return rows;
    }

    private async Task<ReportData> BuildReportAsync(EmployeeListQuery query)
    {
        var all = await Filtered(query).OrderBy(x => x.Id)
            .Take(Reports.RowLimit + 1)
            .Select(Projection)
            .ToListAsync();

        var truncated = all.Count > Reports.RowLimit;
        var shown = all.Take(Reports.RowLimit).ToList();

        var rows = shown.Select((r, i) => (IReadOnlyList<object?>)new object?[]
        {
            i + 1, r.EmployeeId, r.Name, r.Designation, r.Department, r.Salary, r.Concern,
            r.DateOfJoining?.ToString("yyyy-MM-dd"), r.ProbationPeriod,
            r.AfterProbationSalary, r.EffectiveSalary, r.Remarks
        }).ToList();

        return new ReportData(
            "joining-list",
            "Joining List",
            "Employees who joined — entry, salary and probation register.",
            await FilterSummaryAsync(query),
            Columns,
            rows,
            truncated);
    }

    private async Task<string> FilterSummaryAsync(EmployeeListQuery query)
    {
        var parts = new List<string>();

        if (!string.IsNullOrEmpty(query.Search?.Trim()))
        {
            parts.Add($"Search: {query.Search.Trim()}");
        }

        if (query.ConcernId.HasValue)
        {
            var name = await db.Concerns.Where(x => x.Id == query.ConcernId.Value)
                .Select(x => x.Name).FirstOrDefaultAsync();
            parts.Add($"Concern: {name ?? query.ConcernId.Value.ToString()}");
        }

        if (query.DateFrom.HasValue || query.DateTo.HasValue)
        {
            parts.Add($"Joining date: {query.DateFrom?.ToString("dd MMM yyyy") ?? "…"} → {query.DateTo?.ToString("dd MMM yyyy") ?? "…"}");
        }

        return parts.Count == 0 ? "All entries" : string.Join(" · ", parts);
    }

    private static readonly ReportColumn[] Columns =
    [
        new("sl", "SL", "number"),
        new("employeeId", "Employee ID", "text"),
        new("name", "Name", "text"),
        new("designation", "Designation", "text"),
        new("department", "Department", "text"),
        new("salary", "Salary", "money"),
        new("concern", "Concern", "text"),
        new("doj", "Date Of Joining", "date"),
        new("probation", "Probition Period", "text"),
        new("afterProbation", "After Probition Salary", "money"),
        new("effective", "Effective Month", "text"),
        new("remarks", "Remarks", "text")
    ];

    private async Task ApplyAsync(JoiningRecord entity, JoiningSaveRequest request)
    {
        //entity.EmployeeId = Require(request.EmployeeId, "Employee ID", 50);
        entity.EmployeeId = Clean(request.EmployeeId, 50) ?? string.Empty;
        entity.Name = Require(request.Name, "Name", 200);
        entity.Designation = Clean(request.Designation, 200);
        entity.Department = Clean(request.Department, 200);
        entity.Salary = request.Salary;
        entity.DateOfJoining = request.DateOfJoining;
        entity.ProbationPeriod = Clean(request.ProbationPeriod, 100);
        entity.AfterProbationSalary = request.AfterProbationSalary;

        // Fix: Parse string to decimal? for EffectiveSalary
        //if (string.IsNullOrWhiteSpace(request.EffectiveSalary))
        //{
        //    entity.EffectiveSalary = null;
        //}
        //else if (decimal.TryParse(request.EffectiveSalary, out var effSalary))
        //{
        //    entity.EffectiveSalary = effSalary;
        //}
        //else
        //{
        //    throw new DomainException("Effective Salary must be a valid number.");
        //}
        entity.AfterProbationSalary = request.AfterProbationSalary;
        entity.EffectiveSalary = Clean(request.EffectiveSalary, 50);
        entity.Remarks = Clean(request.Remarks, 1000);

       

        if (request.ConcernId.HasValue && !await db.Concerns.AnyAsync(x => x.Id == request.ConcernId.Value))
        {
            throw new DomainException("The selected concern does not exist.");
        }

        entity.ConcernId = request.ConcernId;
    }

    private static System.Linq.Expressions.Expression<Func<JoiningRecord, JoiningRecordDto>> Projection =>
        x => new JoiningRecordDto(
            x.Id, 
            x.EmployeeId, 
            x.Name, 
            x.Designation, 
            x.Department, 
            x.Salary,
            x.ConcernId, 
            x.Concern != null ? x.Concern.Name : null,
            x.DateOfJoining, 
            x.ProbationPeriod, 
            x.AfterProbationSalary,
            x.EffectiveSalary,
            //x.EffectiveSalary != null ? x.EffectiveSalary.ToString() : null, // FIX: convert decimal? to string?
            x.Remarks,
            x.CreatedAtUtc, 
            x.CreatedBy != null ? x.CreatedBy.FullName : null, 
            x.UpdatedAtUtc);

    private static string Require(string? value, string label, int max)
    {
        var text = (value ?? string.Empty).Trim();
        return text.Length == 0
            ? throw new DomainException($"{label} is required.")
            : text.Length <= max ? text : text[..max];
    }

    private static string? Clean(string? value, int max)
    {
        var text = (value ?? string.Empty).Trim();
        return text.Length == 0 ? null : text.Length <= max ? text : text[..max];
    }
}