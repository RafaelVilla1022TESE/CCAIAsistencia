namespace CCAIAsistencia.Models;

/// <summary>
/// Representa un usuario simple para el flujo de autenticación.
/// Incluye los roles asignados para controlar el menú principal.
/// </summary>
public class User
{
    public User()
    {
    }

    public User(string username, string password, string? displayName = null, IEnumerable<string>? roles = null, string? email = null)
    {
        Username = username;
        Password = password;
        DisplayName = string.IsNullOrWhiteSpace(displayName) ? username : displayName;
        Email = email ?? string.Empty;
        RegistrationDate = DateTime.Now;
        Roles = roles?.ToList() ?? new List<string>();
    }

    public int Id { get; set; }

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Alias amigable; si no se define se usa Username.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public DateTime RegistrationDate { get; set; } = DateTime.Now;

    public bool IsActive { get; set; } = true;

    public List<string> Roles { get; set; } = new();
}
