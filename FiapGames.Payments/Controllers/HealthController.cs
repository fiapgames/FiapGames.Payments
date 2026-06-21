using Microsoft.AspNetCore.Mvc;

namespace FiapGames.Payments.Controllers;

[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok("Healthy");
}
