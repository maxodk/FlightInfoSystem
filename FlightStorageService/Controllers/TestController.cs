using Microsoft.AspNetCore.Mvc;

namespace FlightStorageService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { Message = "API is working!" });
    }
}
