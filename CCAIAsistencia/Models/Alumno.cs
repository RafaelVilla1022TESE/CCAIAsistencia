namespace CCAIAsistencia.Models;

/// <summary>
/// Representa un alumno en el sistema.
/// </summary>
public class Alumno
{
    public int UserKey { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public DateTime RegistrationDate { get; set; } = DateTime.Now;

    public bool IsActive { get; set; } = true;

    public byte[]? Fingerprint { get; set; }

    public byte[]? Photo { get; set; }
}
