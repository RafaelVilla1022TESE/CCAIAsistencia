using System.ComponentModel;
using CCAIAsistencia.Controllers;
using CCAIAsistencia.Models;
using CCAIAsistencia.Views;

namespace CCAIAsistencia;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        // Semilla minima de usuarios; en un escenario real vendria de base de datos o API.
        var users = new BindingList<User>
        {
            new("admin", "admin123", "Administrador", new[] { "Usuarios", "Empleados", "Checador", "Reportes" }),
            new("usuario", "clave", "Usuario", new[] { "Checador" })
        };

        // Semilla minima de empleados.
        var students = new BindingList<Alumno>
        {
            new() { UserKey = 1000, FirstName = "Ana", LastName = "Lopez", Email = "ana@example.com", Phone = "555-111-111", Address = "Calle 1" },
            new() { UserKey = 1001, FirstName = "Luis", LastName = "Perez", Email = "luis@example.com", Phone = "555-222-222", Address = "Calle 2" }
        };

        // Datos de ejemplo para reportes.
        var reports = new BindingList<ReportItem>
        {
            new() { Timestamp = DateTime.Now.AddHours(-2), Type = "Entrada", FirstName = "Ana", LastName = "Lopez", Phone = "555-111-111", Email = "ana@example.com" },
            new() { Timestamp = DateTime.Now.AddHours(-1), Type = "Salida", FirstName = "Ana", LastName = "Lopez", Phone = "555-111-111", Email = "ana@example.com" },
            new() { Timestamp = DateTime.Now.AddHours(-3), Type = "Entrada", FirstName = "Luis", LastName = "Perez", Phone = "555-222-222", Email = "luis@example.com" },
            new() { Timestamp = DateTime.Now.AddHours(-1.5), Type = "Salida", FirstName = "Luis", LastName = "Perez", Phone = "555-222-222", Email = "luis@example.com" }
        };

        var controller = new LoginController(users);
        Application.Run(new LoginView(controller, users, students, reports));
    }
}
