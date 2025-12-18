using System.Windows.Forms;
using CCAIAsistencia.Controllers;
using CCAIAsistencia.Data;
using CCAIAsistencia.Models;
using CCAIAsistencia.Views;
using Microsoft.Extensions.Configuration;

namespace CCAIAsistencia;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        var connectionString = configuration.GetConnectionString("Default");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            MessageBox.Show("Falta la cadena de conexion 'Default' en appsettings.json o variables de entorno.", "Configuracion", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        var repository = new AccessControlRepository(connectionString);
        var controller = new LoginController(repository);

        Application.Run(new BootstrapContext(controller, repository));
    }
}

sealed class BootstrapContext : ApplicationContext
{
    private readonly LoginController _controller;
    private readonly AccessControlRepository _repository;

    public BootstrapContext(LoginController controller, AccessControlRepository repository)
    {
        _controller = controller;
        _repository = repository;
        ShowLogin();
    }

    private void ShowLogin()
    {
        var login = new LoginView(_controller, _repository);
        login.LoginSucceeded += OnLoginSucceeded;
        login.FormClosed += OnLoginClosed;
        login.Show();
    }

    private void OnLoginClosed(object? sender, FormClosedEventArgs e)
    {
        ExitThread();
    }

    private void OnLoginSucceeded(object? sender, User user)
    {
        if (sender is LoginView login)
        {
            login.LoginSucceeded -= OnLoginSucceeded;
            login.FormClosed -= OnLoginClosed;
            login.Close();
        }

        var mainMenu = new MainMenuView(user, _repository);
        mainMenu.FormClosed += (_, _) => ExitThread();
        mainMenu.Show();
    }
}
