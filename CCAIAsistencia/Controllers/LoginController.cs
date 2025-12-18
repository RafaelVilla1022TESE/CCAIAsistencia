using CCAIAsistencia.Data;
using CCAIAsistencia.Models;

namespace CCAIAsistencia.Controllers;

/// <summary>
/// Encapsula la lИgica de autenticaciИn contra base de datos.
/// </summary>
public class LoginController
{
    private readonly AccessControlRepository _repository;

    public LoginController(AccessControlRepository repository)
    {
        _repository = repository;
    }

    public bool TryLogin(string username, string password, out string message, out User? authenticatedUser)
    {
        authenticatedUser = null;
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            message = "Ingresa usuario y contraseヵa.";
            return false;
        }

        var user = _repository.Authenticate(username, password);
        if (user is null)
        {
            message = "Usuario no encontrado o inactivo.";
            return false;
        }

        authenticatedUser = user;
        message = $"Bienvenido, {user.DisplayName}.";
        return true;
    }
}
