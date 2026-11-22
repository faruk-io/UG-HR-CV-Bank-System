using CvBank.Api.Auth;
using CvBank.Api.Models;
using CvBank.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CvBank.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
public class DashboardController(DashboardService dashboard) : ControllerBase
{
    [HttpGet("summary")]
    [Permission(Permissions.CandidateView)]
    public async Task<ActionResult<DashboardSummaryDto>> Summary()
    {
        return Ok(await dashboard.SummaryAsync());
    }
}
