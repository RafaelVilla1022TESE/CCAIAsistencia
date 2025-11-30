using System.ComponentModel;
using System.Drawing;
using CCAIAsistencia.Controllers;
using CCAIAsistencia.Models;

namespace CCAIAsistencia.Views;

public partial class LoginView : Form
{
    private readonly LoginController _controller;
    private readonly BindingList<User> _users;
    private readonly BindingList<Alumno> _students;
    private readonly BindingList<ReportItem> _reports;

    public LoginView(LoginController controller, BindingList<User> users, BindingList<Alumno> students, BindingList<ReportItem> reports)
    {
        _controller = controller;
        _users = users;
        _students = students;
        _reports = reports;
        InitializeComponent();
    }

    private void OnLoginClick(object sender, EventArgs e)
    {
        var success = _controller.TryLogin(txtUser.Text, txtPassword.Text, out var message, out var authenticatedUser);
        lblStatus.Text = message;
        lblStatus.ForeColor = success ? Color.ForestGreen : Color.Firebrick;

        if (success)
        {
            MessageBox.Show(message, "Acceso concedido", MessageBoxButtons.OK, MessageBoxIcon.Information);
            var mainMenu = new MainMenuView(authenticatedUser!, _users, _students, _reports);
            mainMenu.FormClosed += (_, _) => Show();
            Hide();
            mainMenu.Show();
        }
    }

    private void OnCancelClick(object sender, EventArgs e)
    {
        Close();
    }
}
