using Microsoft.AspNetCore.Mvc;

namespace CvBank.Api.Controllers;

[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new
    {
        status = "ok",
        app = "CvBank.Api",
        serverTime = DateTime.Now
    });
}
