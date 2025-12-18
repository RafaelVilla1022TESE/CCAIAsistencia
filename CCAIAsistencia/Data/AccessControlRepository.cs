using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using CCAIAsistencia.Models;
using Microsoft.Data.SqlClient;

namespace CCAIAsistencia.Data;

/// <summary>
/// Capa de acceso a datos basada en ADO.NET para el esquema AccessControlDB.
/// </summary>
public class AccessControlRepository
{
    private readonly string _connectionString;

    public AccessControlRepository(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentNullException(nameof(connectionString));
        }

        // Habilitar MARS para evitar conflictos de data readers concurrentes en la misma conexión.
        _connectionString = EnsureMarsEnabled(connectionString);
        EnsureSchema();
    }

    private SqlConnection CreateConnection() => new(_connectionString);

    private void EnsureSchema()
    {
        // Ajuste defensivo: agrega columnas Photo y Profile a Employees y tabla Profiles si no existen.
        const string ensurePhoto = @"
IF COL_LENGTH('dbo.Employees', 'Photo') IS NULL
BEGIN
    ALTER TABLE dbo.Employees ADD Photo VARBINARY(MAX) NULL;
END";

        const string ensureProfile = @"
IF COL_LENGTH('dbo.Employees', 'Profile') IS NULL
BEGIN
    ALTER TABLE dbo.Employees ADD Profile NVARCHAR(100) NULL;
END";

        const string ensureProfilesTable = @"
IF OBJECT_ID('dbo.Profiles', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Profiles(
        Id INT IDENTITY(1,1) PRIMARY KEY,
        ProfileName NVARCHAR(100) NOT NULL UNIQUE,
        IsActive BIT NOT NULL DEFAULT(1)
    );
END";

        const string seedProfiles = @"
MERGE dbo.Profiles AS target
USING (VALUES
    (N'Servicio Social'),
    (N'Residencia'),
    (N'Dual'),
    (N'Estancia'),
    (N'Educación Media Superior'),
    (N'Licenciatura'),
    (N'Maestria'),
    (N'Invitado'),
    (N'Doctorado')
) AS source(ProfileName)
ON target.ProfileName = source.ProfileName
WHEN NOT MATCHED THEN
    INSERT (ProfileName, IsActive) VALUES (source.ProfileName, 1);";

        using var conn = CreateConnection();
        conn.Open();
        using (var cmd = new SqlCommand(ensurePhoto, conn))
        {
            cmd.ExecuteNonQuery();
        }

        using (var cmd = new SqlCommand(ensureProfile, conn))
        {
            cmd.ExecuteNonQuery();
        }

        using (var cmd = new SqlCommand(ensureProfilesTable, conn))
        {
            cmd.ExecuteNonQuery();
        }

        using (var cmd = new SqlCommand(seedProfiles, conn))
        {
            cmd.ExecuteNonQuery();
        }
    }

    #region Users

    public List<string> GetRoles()
    {
        var roles = new List<string>();
        using var conn = CreateConnection();
        conn.Open();
        using var cmd = new SqlCommand("SELECT RoleName FROM Roles ORDER BY RoleName", conn);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            roles.Add(reader.IsDBNull(0) ? string.Empty : reader.GetString(0));
        }

        return roles;
    }

    public List<User> GetUsers()
    {
        var users = new List<User>();
        using var conn = CreateConnection();
        conn.Open();
        using var cmd = new SqlCommand("SELECT Id, UserName, Email, Password, RegistrationDate, IsActive FROM Users", conn);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var user = new User
            {
                Id = reader.GetInt32(0),
                Username = reader.GetString(1),
                DisplayName = reader.GetString(1),
                Email = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                Password = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                RegistrationDate = reader.GetDateTime(4),
                IsActive = reader.GetBoolean(5)
            };
            users.Add(user);
        }

        // Cargar roles ya sin reader abierto para evitar conflictos de cursores.
        foreach (var u in users)
        {
            u.Roles = GetRolesForUser(conn, u.Id);
        }

        return users;
    }

    public User? Authenticate(string username, string password)
    {
        using var conn = CreateConnection();
        conn.Open();
        using var cmd = new SqlCommand(
            @"SELECT Id, UserName, Email, Password, RegistrationDate, IsActive 
              FROM Users 
              WHERE UserName = @username AND Password = @password AND IsActive = 1", conn);
        cmd.Parameters.AddWithValue("@username", username);
        cmd.Parameters.AddWithValue("@password", password);

        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        var user = new User
        {
            Id = reader.GetInt32(0),
            Username = reader.GetString(1),
            DisplayName = reader.GetString(1),
            Email = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
            Password = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
            RegistrationDate = reader.GetDateTime(4),
            IsActive = reader.GetBoolean(5)
        };
        reader.Close();
        user.Roles = GetRolesForUser(conn, user.Id);
        return user;
    }

    public User CreateUser(User user)
    {
        using var conn = CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();

        using (var cmd = new SqlCommand(
                   @"INSERT INTO Users (UserName, Email, Password, RegistrationDate, IsActive) 
                     OUTPUT INSERTED.Id 
                     VALUES (@UserName, @Email, @Password, @RegistrationDate, @IsActive)", conn, tx))
        {
            cmd.Parameters.AddWithValue("@UserName", user.Username);
            cmd.Parameters.AddWithValue("@Email", (object?)user.Email ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Password", user.Password);
            cmd.Parameters.AddWithValue("@RegistrationDate", user.RegistrationDate == default ? DateTime.Now : user.RegistrationDate);
            cmd.Parameters.AddWithValue("@IsActive", user.IsActive);

            user.Id = Convert.ToInt32(cmd.ExecuteScalar());
            user.DisplayName = string.IsNullOrWhiteSpace(user.DisplayName) ? user.Username : user.DisplayName;
        }

        SetUserRoles(conn, tx, user.Id, user.Roles);
        tx.Commit();
        return user;
    }

    public void UpdateUser(User user)
    {
        using var conn = CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();

        using (var cmd = new SqlCommand(
                   @"UPDATE Users 
                     SET UserName = @UserName, Email = @Email, Password = @Password, IsActive = @IsActive 
                     WHERE Id = @Id", conn, tx))
        {
            cmd.Parameters.AddWithValue("@UserName", user.Username);
            cmd.Parameters.AddWithValue("@Email", (object?)user.Email ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Password", user.Password);
            cmd.Parameters.AddWithValue("@IsActive", user.IsActive);
            cmd.Parameters.AddWithValue("@Id", user.Id);
            cmd.ExecuteNonQuery();
        }

        SetUserRoles(conn, tx, user.Id, user.Roles);
        tx.Commit();
    }

    public void DeleteUser(int id)
    {
        using var conn = CreateConnection();
        conn.Open();
        using var cmd = new SqlCommand("DELETE FROM Users WHERE Id = @Id", conn);
        cmd.Parameters.AddWithValue("@Id", id);
        cmd.ExecuteNonQuery();
    }

    private List<string> GetRolesForUser(SqlConnection conn, int userId)
    {
        var roles = new List<string>();
        using var cmd = new SqlCommand(
            @"SELECT r.RoleName 
              FROM UserRoles ur 
              INNER JOIN Roles r ON ur.RoleId = r.Id 
              WHERE ur.UserId = @Id", conn);
        cmd.Parameters.AddWithValue("@Id", userId);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            roles.Add(reader.GetString(0));
        }
        reader.Close();
        return roles;
    }

    public List<string> GetPerfiles()
    {
        var perfiles = new List<string>();
        using var conn = CreateConnection();
        conn.Open();
        using var cmd = new SqlCommand("SELECT ProfileName FROM Profiles WHERE IsActive = 1 ORDER BY ProfileName", conn);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            perfiles.Add(reader.IsDBNull(0) ? string.Empty : reader.GetString(0));
        }

        return perfiles;
    }

    private void SetUserRoles(SqlConnection conn, SqlTransaction tx, int userId, IEnumerable<string> roles)
    {
        using (var clearCmd = new SqlCommand("DELETE FROM UserRoles WHERE UserId = @Id", conn, tx))
        {
            clearCmd.Parameters.AddWithValue("@Id", userId);
            clearCmd.ExecuteNonQuery();
        }

        foreach (var role in roles.Distinct())
        {
            var roleId = EnsureRole(conn, tx, role);
            using var insertCmd = new SqlCommand(
                "INSERT INTO UserRoles (UserId, RoleId, Id) VALUES (@UserId, @RoleId, 0)", conn, tx);
            insertCmd.Parameters.AddWithValue("@UserId", userId);
            insertCmd.Parameters.AddWithValue("@RoleId", roleId);
            insertCmd.ExecuteNonQuery();
        }
    }

    private int EnsureRole(SqlConnection conn, SqlTransaction tx, string roleName)
    {
        using var getCmd = new SqlCommand("SELECT Id FROM Roles WHERE RoleName = @RoleName", conn, tx);
        getCmd.Parameters.AddWithValue("@RoleName", roleName);
        var existing = getCmd.ExecuteScalar();
        if (existing != null)
        {
            return Convert.ToInt32(existing);
        }

        using var insertCmd = new SqlCommand(
            "INSERT INTO Roles (RoleName) OUTPUT INSERTED.Id VALUES (@RoleName)", conn, tx);
        insertCmd.Parameters.AddWithValue("@RoleName", roleName);
        return Convert.ToInt32(insertCmd.ExecuteScalar());
    }

    #endregion

    #region Alumnos

    public List<Alumno> GetAlumnos()
    {
        var alumnos = new List<Alumno>();
        using var conn = CreateConnection();
        conn.Open();
        using var cmd = new SqlCommand(
            @"SELECT Id, UserKey, FirstName, LastName, Email, Phone, Address, Profile, Fingerprint, Photo, RegistrationDate, IsActive 
              FROM Employees", conn);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            alumnos.Add(MapAlumno(reader));
        }

        return alumnos;
    }

    public Alumno? GetAlumnoByMatricula(int matricula)
    {
        using var conn = CreateConnection();
        conn.Open();
        using var cmd = new SqlCommand(
            @"SELECT Id, UserKey, FirstName, LastName, Email, Phone, Address, Profile, Fingerprint, Photo, RegistrationDate, IsActive 
              FROM Employees WHERE UserKey = @Matricula", conn);
        cmd.Parameters.AddWithValue("@Matricula", matricula);
        using var reader = cmd.ExecuteReader();
        return reader.Read() ? MapAlumno(reader) : null;
    }

    public bool ExistsMatricula(int matricula, int? ignoreId = null)
    {
        using var conn = CreateConnection();
        conn.Open();
        using var cmd = new SqlCommand(
            @"SELECT COUNT(1) FROM Employees WHERE UserKey = @Matricula AND (@IgnoreId IS NULL OR Id <> @IgnoreId)", conn);
        cmd.Parameters.AddWithValue("@Matricula", matricula);
        cmd.Parameters.AddWithValue("@IgnoreId", ignoreId.HasValue ? ignoreId.Value : DBNull.Value);
        var count = (int)cmd.ExecuteScalar();
        return count > 0;
    }

    public Alumno CreateAlumno(Alumno alumno)
    {
        using var conn = CreateConnection();
        conn.Open();
        using var cmd = new SqlCommand(
            @"INSERT INTO Employees (UserKey, FirstName, LastName, Email, Phone, Address, Profile, Fingerprint, Photo, RegistrationDate, IsActive) 
              OUTPUT INSERTED.Id 
              VALUES (@Matricula, @FirstName, @LastName, @Email, @Phone, @Address, @Profile, @Fingerprint, @Photo, @RegistrationDate, @IsActive)", conn);
        cmd.Parameters.AddWithValue("@Matricula", alumno.Matricula);
        cmd.Parameters.AddWithValue("@FirstName", alumno.FirstName);
        cmd.Parameters.AddWithValue("@LastName", alumno.LastName);
        cmd.Parameters.AddWithValue("@Email", (object?)alumno.Email ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Phone", (object?)alumno.Phone ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Address", (object?)alumno.Address ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Profile", (object?)alumno.Perfil ?? DBNull.Value);
        cmd.Parameters.Add(BinaryParam("@Fingerprint", alumno.Fingerprint));
        cmd.Parameters.Add(BinaryParam("@Photo", alumno.Photo));
        cmd.Parameters.AddWithValue("@RegistrationDate", alumno.RegistrationDate == default ? DateTime.Now : alumno.RegistrationDate);
        cmd.Parameters.AddWithValue("@IsActive", alumno.IsActive);

        alumno.Id = Convert.ToInt32(cmd.ExecuteScalar());
        return alumno;
    }

    public void UpdateAlumno(Alumno alumno)
    {
        using var conn = CreateConnection();
        conn.Open();
        using var cmd = new SqlCommand(
            @"UPDATE Employees 
              SET UserKey = @Matricula, FirstName = @FirstName, LastName = @LastName, Email = @Email, Phone = @Phone, 
                  Address = @Address, Profile = @Profile, Fingerprint = @Fingerprint, Photo = @Photo, IsActive = @IsActive 
              WHERE Id = @Id", conn);
        cmd.Parameters.AddWithValue("@Matricula", alumno.Matricula);
        cmd.Parameters.AddWithValue("@FirstName", alumno.FirstName);
        cmd.Parameters.AddWithValue("@LastName", alumno.LastName);
        cmd.Parameters.AddWithValue("@Email", (object?)alumno.Email ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Phone", (object?)alumno.Phone ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Address", (object?)alumno.Address ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Profile", (object?)alumno.Perfil ?? DBNull.Value);
        cmd.Parameters.Add(BinaryParam("@Fingerprint", alumno.Fingerprint));
        cmd.Parameters.Add(BinaryParam("@Photo", alumno.Photo));
        cmd.Parameters.AddWithValue("@IsActive", alumno.IsActive);
        cmd.Parameters.AddWithValue("@Id", alumno.Id);
        cmd.ExecuteNonQuery();
    }

    public void DeleteAlumno(int id)
    {
        using var conn = CreateConnection();
        conn.Open();
        using var cmd = new SqlCommand("DELETE FROM Employees WHERE Id = @Id", conn);
        cmd.Parameters.AddWithValue("@Id", id);
        cmd.ExecuteNonQuery();
    }

    private static Alumno MapAlumno(SqlDataReader reader)
    {
        return new Alumno
        {
            Id = reader.GetInt32(0),
            Matricula = reader.GetInt32(1),
            FirstName = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
            LastName = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
            Email = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
            Phone = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
            Address = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
            Perfil = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
            Fingerprint = reader.IsDBNull(8) ? null : ReadBytes(reader, 8),
            Photo = reader.IsDBNull(9) ? null : ReadBytes(reader, 9),
            RegistrationDate = reader.IsDBNull(10) ? DateTime.Now : reader.GetDateTime(10),
            IsActive = reader.GetBoolean(11)
        };
    }

    #endregion

    #region Asistencias

    public int AddAttendanceRecord(int alumnoId, string type)
    {
        using var conn = CreateConnection();
        conn.Open();
        using var cmd = new SqlCommand(
            @"INSERT INTO AttendanceRecords (EmployeeId, Type, Timestamp) 
              OUTPUT INSERTED.Id 
              VALUES (@EmployeeId, @Type, @Timestamp)", conn);
        cmd.Parameters.AddWithValue("@EmployeeId", alumnoId);
        cmd.Parameters.AddWithValue("@Type", type);
        cmd.Parameters.AddWithValue("@Timestamp", DateTime.Now);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public List<ReportItem> GetAttendanceRecords(DateTime? start = null, DateTime? end = null, string? search = null)
    {
        var result = new List<ReportItem>();
        using var conn = CreateConnection();
        conn.Open();

        var sb = new StringBuilder();
        sb.Append(@"SELECT ar.Id, ar.EmployeeId, ar.Type, ar.Timestamp, e.UserKey, e.Profile, e.FirstName, e.LastName, e.Email, e.Phone
                    FROM AttendanceRecords ar
                    INNER JOIN Employees e ON e.Id = ar.EmployeeId
                    WHERE 1 = 1");

        if (start.HasValue)
        {
            sb.Append(" AND ar.Timestamp >= @StartDate");
        }

        if (end.HasValue)
        {
            sb.Append(" AND ar.Timestamp <= @EndDate");
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            sb.Append(@" AND (
                            e.FirstName LIKE @Search OR 
                            e.LastName LIKE @Search OR 
                            e.Email LIKE @Search OR 
                            e.Phone LIKE @Search OR 
                            e.Profile LIKE @Search OR
                            CONVERT(NVARCHAR(20), e.UserKey) LIKE @Search)");
        }

        sb.Append(" ORDER BY ar.Timestamp DESC");

        using var cmd = new SqlCommand(sb.ToString(), conn);
        if (start.HasValue)
        {
            cmd.Parameters.AddWithValue("@StartDate", start.Value);
        }

        if (end.HasValue)
        {
            cmd.Parameters.AddWithValue("@EndDate", end.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            cmd.Parameters.AddWithValue("@Search", $"%{search}%");
        }

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new ReportItem
            {
                Id = reader.GetInt32(0),
                AlumnoId = reader.GetInt32(1),
                Type = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                Timestamp = reader.GetDateTime(3),
                Matricula = reader.GetInt32(4),
                Perfil = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                FirstName = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                LastName = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
                Email = reader.IsDBNull(8) ? string.Empty : reader.GetString(8),
                Phone = reader.IsDBNull(9) ? string.Empty : reader.GetString(9)
            });
        }

        return result;
    }

    #endregion

    private static byte[] ReadBytes(SqlDataReader reader, int ordinal)
    {
        using var ms = new MemoryStream();
        const int bufferSize = 1024;
        long dataIndex = 0;
        byte[] buffer = new byte[bufferSize];
        long bytesRead;
        while ((bytesRead = reader.GetBytes(ordinal, dataIndex, buffer, 0, buffer.Length)) > 0)
        {
            ms.Write(buffer, 0, (int)bytesRead);
            dataIndex += bytesRead;
        }
        return ms.ToArray();
    }

    private static SqlParameter BinaryParam(string name, byte[]? data)
    {
        return new SqlParameter(name, SqlDbType.VarBinary) { Value = (object?)data ?? DBNull.Value };
    }

    private static string EnsureMarsEnabled(string connectionString)
    {
        // Evita agregar duplicado si ya viene en la cadena.
        if (connectionString.IndexOf("MultipleActiveResultSets", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return connectionString;
        }

        var separator = connectionString.Trim().EndsWith(";") ? string.Empty : ";";
        return $"{connectionString}{separator}MultipleActiveResultSets=True;";
    }
}
