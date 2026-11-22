using CvBank.Api.Auth;
using CvBank.Api.Data;
using CvBank.Api.Models;
using CvBank.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CvBank.Api.Controllers;

[ApiController]
[Route("api/resigned")]
[Authorize]
public class ResignedController(CvBankDbContext db, IAuditWriter audit) : ControllerBase
{
    private SignedInUser Me => SignedInUser.From(User);

    [HttpGet]
    public async Task<ActionResult<PagedResult<ResignedRecordDto>>> List([FromQuery] EmployeeListQuery query)
    {
        var rows = Filtered(query);

        var total = await rows.CountAsync();
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var page = Math.Max(1, query.Page);

        var items = await rows.OrderBy(x => x.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(Projection)
            .ToListAsync();

        return Ok(new PagedResult<ResignedRecordDto>(items, total, page, pageSize));
    }

    [HttpGet("export")]
    public async Task<ActionResult<ReportData>> Export([FromQuery] EmployeeListQuery query)
    {
        var report = await BuildReportAsync(query);

        await audit.WriteAsync(Me.UserId, Me.FullName, "Resigned PDF Export", "ResignedRecord", null,
            $"{report.Rows.Count} rows · {report.FilterSummary}");

        return Ok(report);
    }

    [HttpGet("excel")]
    public async Task<IActionResult> Excel([FromQuery] EmployeeListQuery query)
    {
        var report = await BuildReportAsync(query);
        var bytes = ReportExcel.Write(report, Me.FullName);

        await audit.WriteAsync(Me.UserId, Me.FullName, "Resigned Excel Export", "ResignedRecord", null,
            $"{ReportExcel.FileName(report)} · {report.Rows.Count} rows · {report.FilterSummary}");

        return File(bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ReportExcel.FileName(report));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ResignedRecordDto>> Get(int id)
    {
        var dto = await db.ResignedRecords.Where(x => x.Id == id).Select(Projection).FirstOrDefaultAsync()
            ?? throw new NotFoundException("Resigned entry not found.");

        return Ok(dto);
    }

    [HttpPost]
    public async Task<ActionResult<ResignedRecordDto>> Create(ResignedSaveRequest request)
    {
        var entity = new ResignedRecord
        {
            CreatedAtUtc = DateTime.UtcNow,
            CreatedById = Me.UserId
        };

        await ApplyAsync(entity, request);
        db.ResignedRecords.Add(entity);
        await db.SaveChangesAsync();

        await audit.WriteAsync(Me.UserId, Me.FullName, "Resigned Create", "ResignedRecord",
            entity.Id.ToString(), $"{entity.EmployeeId} | {entity.Name}");

        return await Get(entity.Id);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ResignedRecordDto>> Update(int id, ResignedSaveRequest request)
    {
        var entity = await db.ResignedRecords.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new NotFoundException("Resigned entry not found.");

        await ApplyAsync(entity, request);
        entity.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();

        await audit.WriteAsync(Me.UserId, Me.FullName, "Resigned Update", "ResignedRecord",
            entity.Id.ToString(), $"{entity.EmployeeId} | {entity.Name}");

        return await Get(entity.Id);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var entity = await db.ResignedRecords.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new NotFoundException("Resigned entry not found.");

        db.ResignedRecords.Remove(entity);
        await db.SaveChangesAsync();

        await audit.WriteAsync(Me.UserId, Me.FullName, "Resigned Delete", "ResignedRecord",
            id.ToString(), $"{entity.EmployeeId} | {entity.Name}");

        return NoContent();
    }

    private IQueryable<ResignedRecord> Filtered(EmployeeListQuery query)
    {
        var rows = db.ResignedRecords.AsQueryable();

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
            rows = rows.Where(x => x.ResignDate >= from);
        }

        if (query.DateTo.HasValue)
        {
            var toExclusive = query.DateTo.Value.ToDateTime(TimeOnly.MinValue).AddDays(1);
            rows = rows.Where(x => x.ResignDate < toExclusive);
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
            r.DateOfJoining?.ToString("yyyy-MM-dd"), r.ResignDate?.ToString("yyyy-MM-dd"),
            r.LastWorkingDay?.ToString("yyyy-MM-dd"), r.EffectiveFrom?.ToString("yyyy-MM-dd"),
            r.NoticePeriod, r.HrApproved ? "Yes" : "No", r.CauseOfResignation, r.Remarks
        }).ToList();

        return new ReportData(
            "resigned-list",
            "Resigned List",
            "Employees who resigned — dates, notice period and HR approval register.",
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
            parts.Add($"Resign date: {query.DateFrom?.ToString("dd MMM yyyy") ?? "…"} → {query.DateTo?.ToString("dd MMM yyyy") ?? "…"}");
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
        new("resignDate", "Resign Date", "date"),
        new("lwd", "Last Working Day", "date"),
        new("effectiveFrom", "Effective From", "date"),
        new("notice", "Notice Period", "text"),
        new("hrApproved", "HR Approved", "text"),
        new("cause", "Convers of Resignation", "text"),
        new("remarks", "Remarks", "text")
    ];

    private async Task ApplyAsync(ResignedRecord entity, ResignedSaveRequest request)
    {
        entity.EmployeeId = Require(request.EmployeeId, "Employee ID", 50);
        entity.Name = Require(request.Name, "Name", 200);
        entity.Designation = Clean(request.Designation, 200);
        entity.Department = Clean(request.Department, 200);
        entity.Salary = request.Salary;
        entity.DateOfJoining = request.DateOfJoining;
        entity.ResignDate = request.ResignDate;
        entity.LastWorkingDay = request.LastWorkingDay;
        entity.EffectiveFrom = request.EffectiveFrom;
        entity.NoticePeriod = Clean(request.NoticePeriod, 100);
        entity.HrApproved = request.HrApproved;
        entity.CauseOfResignation = Clean(request.CauseOfResignation, 500);
        entity.Remarks = Clean(request.Remarks, 1000);

        if (request.ConcernId.HasValue && !await db.Concerns.AnyAsync(x => x.Id == request.ConcernId.Value))
        {
            throw new DomainException("The selected concern does not exist.");
        }

        entity.ConcernId = request.ConcernId;
    }

    private static System.Linq.Expressions.Expression<Func<ResignedRecord, ResignedRecordDto>> Projection =>
        x => new ResignedRecordDto(
            x.Id, x.EmployeeId, x.Name, x.Designation, x.Department, x.Salary,
            x.ConcernId, x.Concern != null ? x.Concern.Name : null,
            x.DateOfJoining, x.ResignDate, x.LastWorkingDay, x.EffectiveFrom,
            x.NoticePeriod, x.HrApproved, x.CauseOfResignation, x.Remarks,
            x.CreatedAtUtc, x.CreatedBy != null ? x.CreatedBy.FullName : null, x.UpdatedAtUtc);

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