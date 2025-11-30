using CCAIAsistencia.Models;

namespace CCAIAsistencia.Controllers;

/// <summary>
/// Encapsula la lógica de autenticación.
/// </summary>
public class LoginController
{
    private readonly IList<User> _users;

    public LoginController(IList<User> users)
    {
        _users = users;
    }

    public bool TryLogin(string username, string password, out string message, out User? authenticatedUser)
    {
        authenticatedUser = null;
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            message = "Ingresa usuario y contraseña.";
            return false;
        }

        var user = _users.FirstOrDefault(u => string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase));
        if (user is null)
        {
            message = "Usuario no encontrado.";
            return false;
        }

        if (!string.Equals(user.Password, password))
        {
            message = "Contraseña incorrecta.";
            return false;
        }

        authenticatedUser = user;
        message = $"Bienvenido, {user.DisplayName}.";
        return true;
    }
}
