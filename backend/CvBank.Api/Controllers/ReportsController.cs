using CvBank.Api.Auth;
using CvBank.Api.Models;
using CvBank.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CvBank.Api.Controllers;

[ApiController]
[Route("api/reports")]
public class ReportsController(ReportService reports, IAuditWriter audit) : ControllerBase
{
    private SignedInUser Me => SignedInUser.From(User);

    [HttpGet]
    [Permission(Permissions.ReportView)]
    public ActionResult<IReadOnlyList<ReportDefinition>> List() => Ok(Reports.Menu);

    [HttpGet("{code}")]
    [Permission(Permissions.ReportView)]
    public async Task<ActionResult<ReportData>> Get(string code, [FromQuery] ReportQuery query)
    {
        var report = await reports.BuildAsync(code, query);

        await audit.WriteAsync(Me.UserId, Me.FullName, "Report View", "Report", report.Code,
            $"{report.Title} · {report.Rows.Count} rows · {report.FilterSummary}");

        return Ok(report);
    }

    [HttpGet("{code}/excel")]
    [Permission(Permissions.ReportView)]
    public async Task<IActionResult> Excel(string code, [FromQuery] ReportQuery query)
    {
        var report = await reports.BuildAsync(code, query);
        var bytes = ReportExcel.Write(report, Me.FullName);

        await audit.WriteAsync(Me.UserId, Me.FullName, "Report Excel Export", "Report", report.Code,
            $"{report.Title} · {ReportExcel.FileName(report)} · {report.Rows.Count} rows");

        return File(bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ReportExcel.FileName(report));
    }
}
