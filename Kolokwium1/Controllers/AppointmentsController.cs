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
    
    [HttpPost]
    public async Task<IActionResult> AddAppointment([FromBody] AddAppointmentRequest request)
    {
        if (request == null || request.Services == null || request.Services.Count == 0)
        {
            return BadRequest(new { message = "Invalid request: missing data or services." });
        }

        var error = await _appointmentsService.AddAppointmentAsync(request);

        if (error != null)
        {

            if (error.Contains("Appointment already exists"))
                return Conflict(new { message = error });

            if (error.Contains("Patient does not exist") || error.Contains("Doctor does not exist") || error.Contains("Service not found"))
                return NotFound(new { message = error });

            return BadRequest(new { message = error });
        }
        
        return Ok("Appointment added");
    }


}
