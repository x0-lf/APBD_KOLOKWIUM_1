namespace Kolokwium1.Controllers;

public class AddAppointmentRequest
{
    public int AppointmentId { get; set; }
    public int PatientId { get; set; }
    public string PWZ { get; set; } = string.Empty;
    public List<AddServiceDTO> Services { get; set; } = new();
}