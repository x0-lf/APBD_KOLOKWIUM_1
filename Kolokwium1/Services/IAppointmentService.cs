namespace Kolokwium1.Services;

public interface IAppointmentsService
{
    Task<AppointmentDTO> GetAppointmentByIdAsync(int id);
}