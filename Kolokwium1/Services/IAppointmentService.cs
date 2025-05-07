using Kolokwium1.Controllers;

namespace Kolokwium1.Services;

public interface IAppointmentsService
{
    Task<AppointmentDTO> GetAppointmentByIdAsync(int id);
    Task<string> AddAppointmentAsync(AddAppointmentRequest request);
}