using Kolokwium1.Services;
using Microsoft.AspNetCore.Mvc;

namespace Kolokwium1.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AppointmentsController : ControllerBase
{
    private readonly IAppointmentsService _appointmentsService;
    private readonly IConfiguration _configuration;

    public AppointmentsController(IAppointmentsService appointmentsService, IConfiguration configuration)
    {
        _configuration = configuration;
        _appointmentsService = appointmentsService;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetAppointment(int id)
    {
        var appointment = await _appointmentsService.GetAppointmentByIdAsync(id);
        if (appointment == null)
            return NotFound();

        return Ok(appointment);
    }
}
