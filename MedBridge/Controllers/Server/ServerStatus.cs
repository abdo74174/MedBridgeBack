using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    [Route("status")]
    public IActionResult GetStatus()
    {
        return Ok(new { status = "Server is online" });
    }
}
