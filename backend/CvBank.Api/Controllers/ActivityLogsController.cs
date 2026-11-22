using CvBank.Api.Auth;
using CvBank.Api.Models;
using CvBank.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CvBank.Api.Controllers;

[ApiController]
[Route("api/activity-logs")]
public class ActivityLogsController(ActivityLogService logs, IAuditWriter audit) : ControllerBase
{
    private SignedInUser Me => SignedInUser.From(User);

    [HttpGet("options")]
    [Permission(Permissions.ActivityLogView)]
    public async Task<ActionResult<ActivityLogOptions>> Options() => Ok(await logs.OptionsAsync());

    [HttpGet]
    [Permission(Permissions.ActivityLogView)]
    public async Task<ActionResult<ReportData>> Get([FromQuery] ActivityLogQuery query)
    {
        var log = await logs.BuildAsync(query);

        await audit.WriteAsync(Me.UserId, Me.FullName, "Activity Log View", "AuditLog", null,
            $"{log.Rows.Count} rows · {log.FilterSummary}");

        return Ok(log);
    }

    [HttpGet("excel")]
    [Permission(Permissions.ActivityLogView)]
    public async Task<IActionResult> Excel([FromQuery] ActivityLogQuery query)
    {
        var log = await logs.BuildAsync(query);
        var bytes = ReportExcel.Write(log, Me.FullName);

        await audit.WriteAsync(Me.UserId, Me.FullName, "Activity Log Excel Export", "AuditLog", null,
            $"{ReportExcel.FileName(log)} · {log.Rows.Count} rows · {log.FilterSummary}");

        return File(bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ReportExcel.FileName(log));
    }
}
