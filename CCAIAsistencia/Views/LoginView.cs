using System.Drawing;
using CCAIAsistencia.Data;
using CCAIAsistencia.Controllers;
using CCAIAsistencia.Models;

namespace CCAIAsistencia.Views;

public partial class LoginView : Form
{
    private readonly LoginController _controller;
    private readonly AccessControlRepository _repository;

    public event EventHandler<User>? LoginSucceeded;

    public LoginView(LoginController controller, AccessControlRepository repository)
    {
        _controller = controller;
        _repository = repository;
        InitializeComponent();
    }

    private void OnLoginClick(object sender, EventArgs e)
    {
        var success = _controller.TryLogin(txtUser.Text, txtPassword.Text, out var message, out var authenticatedUser);
        lblStatus.Text = message;
        lblStatus.ForeColor = success ? Color.ForestGreen : Color.Firebrick;

        if (success)
        {
            LoginSucceeded?.Invoke(this, authenticatedUser!);
        }
    }

    private void OnCancelClick(object sender, EventArgs e)
    {
        Close();
    }
}
