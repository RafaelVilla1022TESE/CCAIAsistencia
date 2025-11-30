namespace CCAIAsistencia.Models;

/// <summary>
/// Representa un usuario simple para el flujo de autenticación.
/// Incluye los roles asignados para controlar el menú principal.
/// </summary>
public class User
{
    public User(string username, string password, string displayName, IEnumerable<string>? roles = null)
    {
        Username = username;
        Password = password;
        DisplayName = displayName;
        Roles = roles?.ToList() ?? new List<string>();
    }

    public string Username { get; set; }

    public string Password { get; set; }

    public string DisplayName { get; set; }

    public List<string> Roles { get; set; }
}
