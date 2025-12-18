using System.ComponentModel;
using System.Drawing;
using System.Linq;
using CCAIAsistencia.Data;
using CCAIAsistencia.Models;
using CCAIAsistencia.Utils;

namespace CCAIAsistencia.Views;

/// <summary>
/// Formulario para crear o editar usuarios persistidos en base de datos.
/// </summary>
public class UserForm : Form
{
    private readonly AccessControlRepository _repository;
    private readonly User? _userToEdit;
    private readonly List<string> _availableRoles;

    private TextBox _txtUsername = null!;
    private TextBox _txtPassword = null!;
    private TextBox _txtEmail = null!;
    private CheckedListBox _rolesList = null!;
    private Button _btnSave = null!;
    private Button _btnCancel = null!;

    public UserForm(AccessControlRepository repository, User? userToEdit)
    {
        _repository = repository;
        _userToEdit = userToEdit;
        _availableRoles = _repository.GetRoles();
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
        ClientSize = new Size(420, 340);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Icon = IconLoader.Load("user_add");

        var lblUser = new Label { Text = "Usuario", Location = new Point(20, 20), AutoSize = true };
        _txtUsername = new TextBox { Location = new Point(120, 16), Width = 260, MaxLength = 100 };

        var lblPassword = new Label { Text = "Contrase\u00f1a", Location = new Point(20, 65), AutoSize = true };
        _txtPassword = new TextBox { Location = new Point(120, 61), Width = 260, MaxLength = 200, PasswordChar = '*' };

        var lblEmail = new Label { Text = "Email", Location = new Point(20, 110), AutoSize = true };
        _txtEmail = new TextBox { Location = new Point(120, 106), Width = 260, MaxLength = 150 };

        var lblRoles = new Label { Text = "Roles", Location = new Point(20, 155), AutoSize = true };
        _rolesList = new CheckedListBox
        {
            Location = new Point(120, 150),
            Width = 260,
            Height = 110,
            CheckOnClick = true
        };

        _btnSave = new Button { Text = "Guardar", Location = new Point(120, 280), Width = 100 };
        _btnSave.Click += (_, _) => SaveUser();

        _btnCancel = new Button { Text = "Cancelar", Location = new Point(240, 280), Width = 100 };
        _btnCancel.Click += (_, _) => DialogResult = DialogResult.Cancel;

        Controls.Add(lblUser);
        Controls.Add(_txtUsername);
        Controls.Add(lblPassword);
        Controls.Add(_txtPassword);
        Controls.Add(lblEmail);
        Controls.Add(_txtEmail);
        Controls.Add(lblRoles);
        Controls.Add(_rolesList);
        Controls.Add(_btnSave);
        Controls.Add(_btnCancel);
    }

    private void LoadRoles()
    {
        foreach (var role in _availableRoles)
        {
            _rolesList.Items.Add(role, false);
        }
    }

    private void LoadData()
    {
        _txtUsername.Text = _userToEdit!.Username;
        _txtPassword.Text = _userToEdit.Password;
        _txtEmail.Text = _userToEdit.Email;

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
        var email = _txtEmail.Text.Trim();
        var selectedRoles = _rolesList.CheckedItems.Cast<string>().ToList();

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            MessageBox.Show("Usuario y contrase\u00f1a son obligatorios.", "Validaci\u00f3n", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (selectedRoles.Count == 0)
        {
            MessageBox.Show("Selecciona al menos un rol.", "Validaci\u00f3n", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var existingUsers = _repository.GetUsers();
        if (_userToEdit == null)
        {
            if (existingUsers.Any(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show("Ya existe un usuario con ese nombre.", "Validaci\u00f3n", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var newUser = new User(username, password, username, selectedRoles, email);
            _repository.CreateUser(newUser);
        }
        else
        {
            if (existingUsers.Any(u => u.Id != _userToEdit.Id && u.Username.Equals(username, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show("Ya existe un usuario con ese nombre.", "Validaci\u00f3n", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _userToEdit.Username = username;
            _userToEdit.Password = password;
            _userToEdit.Email = email;
            _userToEdit.DisplayName = string.IsNullOrWhiteSpace(_userToEdit.DisplayName) ? username : _userToEdit.DisplayName;
            _userToEdit.Roles = selectedRoles;
            _repository.UpdateUser(_userToEdit);
        }

        DialogResult = DialogResult.OK;
    }
}
