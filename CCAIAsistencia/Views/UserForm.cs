using System.ComponentModel;
using System.Drawing;
using System.Linq;
using CCAIAsistencia.Models;

namespace CCAIAsistencia.Views;

/// <summary>
/// Formulario para crear o editar usuarios en memoria.
/// </summary>
public class UserForm : Form
{
    private readonly BindingList<User> _users;
    private readonly User? _userToEdit;

    private TextBox _txtUsername = null!;
    private TextBox _txtPassword = null!;
    private TextBox _txtDisplayName = null!;
    private CheckedListBox _rolesList = null!;
    private Button _btnSave = null!;
    private Button _btnCancel = null!;

    private static readonly string[] AvailableRoles = { "Usuarios", "Empleados", "Checador", "Reportes" };

    public UserForm(BindingList<User> users, User? userToEdit)
    {
        _users = users;
        _userToEdit = userToEdit;
        InitializeComponent();
        LoadRoles();
        if (_userToEdit != null)
        {
            LoadData();
        }
    }

    private void InitializeComponent()
    {
        Text = _userToEdit == null ? "Nuevo usuario" : "Editar usuario";
        ClientSize = new Size(420, 360);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        var lblUser = new Label { Text = "Usuario", Location = new Point(20, 20), AutoSize = true };
        _txtUsername = new TextBox { Location = new Point(120, 16), Width = 260, MaxLength = 100 };

        var lblPassword = new Label { Text = "Contraseña", Location = new Point(20, 65), AutoSize = true };
        _txtPassword = new TextBox { Location = new Point(120, 61), Width = 260, MaxLength = 200, PasswordChar = '*' };

        var lblDisplayName = new Label { Text = "Nombre", Location = new Point(20, 110), AutoSize = true };
        _txtDisplayName = new TextBox { Location = new Point(120, 106), Width = 260, MaxLength = 150 };

        var lblRoles = new Label { Text = "Roles", Location = new Point(20, 155), AutoSize = true };
        _rolesList = new CheckedListBox
        {
            Location = new Point(120, 150),
            Width = 260,
            Height = 110,
            CheckOnClick = true
        };

        _btnSave = new Button { Text = "Guardar", Location = new Point(120, 290), Width = 100 };
        _btnSave.Click += (_, _) => SaveUser();

        _btnCancel = new Button { Text = "Cancelar", Location = new Point(240, 290), Width = 100 };
        _btnCancel.Click += (_, _) => DialogResult = DialogResult.Cancel;

        Controls.Add(lblUser);
        Controls.Add(_txtUsername);
        Controls.Add(lblPassword);
        Controls.Add(_txtPassword);
        Controls.Add(lblDisplayName);
        Controls.Add(_txtDisplayName);
        Controls.Add(lblRoles);
        Controls.Add(_rolesList);
        Controls.Add(_btnSave);
        Controls.Add(_btnCancel);
    }

    private void LoadRoles()
    {
        foreach (var role in AvailableRoles)
        {
            _rolesList.Items.Add(role, false);
        }
    }

    private void LoadData()
    {
        _txtUsername.Text = _userToEdit!.Username;
        _txtPassword.Text = _userToEdit.Password;
        _txtDisplayName.Text = _userToEdit.DisplayName;

        for (int i = 0; i < _rolesList.Items.Count; i++)
        {
            if (_userToEdit.Roles.Contains(_rolesList.Items[i]!.ToString()!))
            {
                _rolesList.SetItemChecked(i, true);
            }
        }
    }

    private void SaveUser()
    {
        var username = _txtUsername.Text.Trim();
        var password = _txtPassword.Text;
        var displayName = _txtDisplayName.Text.Trim();
        var selectedRoles = _rolesList.CheckedItems.Cast<string>().ToList();

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            MessageBox.Show("Usuario y contraseña son obligatorios.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (selectedRoles.Count == 0)
        {
            MessageBox.Show("Selecciona al menos un rol.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (_userToEdit == null)
        {
            if (_users.Any(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show("Ya existe un usuario con ese nombre.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _users.Add(new User(username, password, string.IsNullOrWhiteSpace(displayName) ? username : displayName, selectedRoles));
        }
        else
        {
            if (_users.Any(u => u != _userToEdit && u.Username.Equals(username, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show("Ya existe un usuario con ese nombre.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _userToEdit.Username = username;
            _userToEdit.Password = password;
            _userToEdit.DisplayName = string.IsNullOrWhiteSpace(displayName) ? username : displayName;
            _userToEdit.Roles = selectedRoles;
        }

        DialogResult = DialogResult.OK;
    }
}
