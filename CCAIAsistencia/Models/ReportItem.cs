namespace CCAIAsistencia.Models;

/// <summary>
/// Representa un registro de asistencia para reportes.
/// </summary>
public class ReportItem
{
    public DateTime Timestamp { get; set; }

    public string Type { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;
}
