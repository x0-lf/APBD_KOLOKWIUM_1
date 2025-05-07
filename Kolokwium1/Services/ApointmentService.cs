using Kolokwium1.Controllers;
using Microsoft.Data.SqlClient;

namespace Kolokwium1.Services;

public class AppointmentsService : IAppointmentsService
{
    private readonly IConfiguration _configuration;

    public AppointmentsService(IConfiguration configuration)
    {
        _configuration = configuration;
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

    public async Task<string> AddAppointmentAsync(AddAppointmentRequest request)
    {
        var defaultConnection = _configuration.GetConnectionString("DefaultConnection");
        await using var conn = new SqlConnection(defaultConnection);
        await conn.OpenAsync();

        var appointmentCmd = new SqlCommand("SELECT appoitment_id FROM Appointment WHERE appoitment_id = @id", conn);
        appointmentCmd.Parameters.AddWithValue("@id", request.AppointmentId);
        if ((await appointmentCmd.ExecuteScalarAsync()) != null)
            return "Appointment already exists";

        var checkPatientCmd = new SqlCommand("SELECT patient_id FROM Patient WHERE patient_id = @id", conn);
        checkPatientCmd.Parameters.AddWithValue("@id", request.PatientId);
        if ((await checkPatientCmd.ExecuteScalarAsync()) == null)
            return "Patient does not exist";

        var checkDoctorCmd = new SqlCommand("SELECT doctor_id FROM Doctor WHERE pwz = @pwz", conn);
        checkDoctorCmd.Parameters.AddWithValue("@pwz", request.PWZ);
        var doctorIdObj = await checkDoctorCmd.ExecuteScalarAsync();
        if (doctorIdObj == null)
            return "Doctor does not exist";
        int doctorId = (int)doctorIdObj;

        foreach (var service in request.Services)
        {
            var serviceCheckCmd = new SqlCommand("SELECT service_id FROM Service WHERE name = @name", conn);
            serviceCheckCmd.Parameters.AddWithValue("@name", service.ServiceName);
            if ((await serviceCheckCmd.ExecuteScalarAsync()) == null)
                return $"Service not found: {service.ServiceName}";
        }

        var insertAppointmentCmd = new SqlCommand(@"
        INSERT INTO Appointment (appoitment_id, patient_id, doctor_id, date)
        VALUES (@id, @pid, @did, GETDATE())", conn);
        insertAppointmentCmd.Parameters.AddWithValue("@id", request.AppointmentId);
        insertAppointmentCmd.Parameters.AddWithValue("@pid", request.PatientId);
        insertAppointmentCmd.Parameters.AddWithValue("@did", doctorId);
        await insertAppointmentCmd.ExecuteNonQueryAsync();

        foreach (var service in request.Services)
        {
            var getServiceIdCmd = new SqlCommand("SELECT service_id FROM Service WHERE name = @name", conn);
            getServiceIdCmd.Parameters.AddWithValue("@name", service.ServiceName);
            int serviceId = (int)(await getServiceIdCmd.ExecuteScalarAsync())!;

            var insertServiceCmd = new SqlCommand(@"
            INSERT INTO Appointment_Service (appoitment_id, service_id, service_fee)
            VALUES (@aid, @sid, @fee)", conn);
            insertServiceCmd.Parameters.AddWithValue("@aid", request.AppointmentId);
            insertServiceCmd.Parameters.AddWithValue("@sid", serviceId);
            insertServiceCmd.Parameters.AddWithValue("@fee", service.ServiceFee);
            await insertServiceCmd.ExecuteNonQueryAsync();
        }

        return null; 
    }
    
}
