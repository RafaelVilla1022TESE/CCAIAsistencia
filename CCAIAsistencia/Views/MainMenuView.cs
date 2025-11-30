using System.ComponentModel;
using System.Drawing;
using CCAIAsistencia.Models;

namespace CCAIAsistencia.Views;

/// <summary>
/// Ventana principal tipo MDI que muestra menus segun roles del usuario.
/// </summary>
public class MainMenuView : Form
{
    private readonly User _user;
    private readonly BindingList<User> _users;
    private readonly BindingList<Alumno> _students;
    private readonly BindingList<ReportItem> _reports;

    private MenuStrip _menuStrip = null!;
    private ToolStripMenuItem _usuariosMenu = null!;
    private ToolStripMenuItem _empleadosMenu = null!;
    private ToolStripMenuItem _checadorMenu = null!;
    private ToolStripMenuItem _reportesMenu = null!;
    private ToolStripMenuItem _salirMenu = null!;

    public MainMenuView(User user, BindingList<User> users, BindingList<Alumno> students, BindingList<ReportItem> reports)
    {
        _user = user;
        _users = users;
        _students = students;
        _reports = reports;
        InitializeComponent();
        ApplyRoleVisibility();
    }

    private void ApplyRoleVisibility()
    {
        if (_user.Roles.Count == 0)
        {
            return;
        }

        foreach (var role in _user.Roles)
        {
            switch (role.ToLowerInvariant())
            {
                case "usuarios":
                    _usuariosMenu.Visible = true;
                    break;
                case "empleados":
                    _empleadosMenu.Visible = true;
                    break;
                case "checador":
                    _checadorMenu.Visible = true;
                    break;
                case "reportes":
                    _reportesMenu.Visible = true;
                    break;
            }
        }
    }

    private void InitializeComponent()
    {
        _menuStrip = new MenuStrip();
        _usuariosMenu = new ToolStripMenuItem { Text = "Usuarios", Visible = false };
        _empleadosMenu = new ToolStripMenuItem { Text = "Empleados", Visible = false };
        _checadorMenu = new ToolStripMenuItem { Text = "Checador", Visible = false };
        _reportesMenu = new ToolStripMenuItem { Text = "Reportes", Visible = false };
        _salirMenu = new ToolStripMenuItem { Text = "Salir" };

        _usuariosMenu.Click += (_, _) => OpenChild(new UserListView(_users));
        _empleadosMenu.Click += (_, _) => OpenChild(new AlumnoListView(_students));
        _checadorMenu.Click += (_, _) => OpenChild(new CheckInForm(_students, _reports));
        _reportesMenu.Click += (_, _) => OpenChild(new ReportsForm(_reports));
        _salirMenu.Click += (_, _) => Close();

        _menuStrip.Items.AddRange(new ToolStripItem[]
        {
            _usuariosMenu,
            _empleadosMenu,
            _checadorMenu,
            _reportesMenu,
            _salirMenu
        });

        Controls.Add(_menuStrip);
        MainMenuStrip = _menuStrip;
        IsMdiContainer = true;
        Text = "CCAIAsistencia - Menu Principal";
        WindowState = FormWindowState.Maximized;
        MinimumSize = new Size(820, 500);
        ClientSize = new Size(900, 600);
    }

    private void OpenChild(Form child)
    {
        child.MdiParent = this;
        child.Show();
        child.BringToFront();
    }
}
