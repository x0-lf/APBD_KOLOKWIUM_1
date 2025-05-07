using Kolokwium1.Controllers;
using Microsoft.Data.SqlClient;

namespace Kolokwium1.Services;

public class AppointmentsService : IAppointmentsService
{
    private readonly IConfiguration _configuration;

    public AppointmentsService(IConfiguration configuration)
    {
        _configuration = configuration;
        // _configuration.GetConnectionString("DefaultConnection");
    }

public async Task<AppointmentDTO> GetAppointmentByIdAsync(int id)
{
    var defaultConnection = _configuration.GetConnectionString("DefaultConnection");
    
    await using var connection = new SqlConnection(defaultConnection);
    await connection.OpenAsync();
    
    await using (var command = new SqlCommand(@"
        SELECT a.date, p.first_name, p.last_name, p.date_of_birth,
               d.doctor_id, d.pwz
        FROM Appointment a
        JOIN Patient p ON p.patient_id = a.patient_id
        JOIN Doctor d ON d.doctor_id = a.doctor_id
        WHERE a.appoitment_id = @id", connection))
    {
        command.Parameters.AddWithValue("@id", id);

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return null;

        var appointment = new AppointmentDTO
        {
            Date = reader.GetDateTime(0),
            Patient = new PatientDTO
            {
                FirstName = reader.GetString(1),
                LastName = reader.GetString(2),
                DateOfBirth = reader.GetDateTime(3)
            },
            Doctor = new DoctorDTO
            {
                DoctorId = reader.GetInt32(4),
                PWZ = reader.GetString(5)
            },
            AppointmentServices = new List<AppointmentServiceDTO>()
        };

        reader.Close();

        await using (var serviceCommand = new SqlCommand(@"
            SELECT s.name, aps.service_fee
            FROM Appointment_Service aps
            JOIN Service s ON s.service_id = aps.service_id
            WHERE aps.appoitment_id = @id", connection))
        {
            serviceCommand.Parameters.AddWithValue("@id", id);

            await using var serviceReader = await serviceCommand.ExecuteReaderAsync();
            while (await serviceReader.ReadAsync())
            {
                appointment.AppointmentServices.Add(new AppointmentServiceDTO
                {
                    Name = serviceReader.GetString(0),
                    ServiceFee = serviceReader.GetDecimal(1)
                });
            }
        }

        return appointment;
    }
}

    
}
