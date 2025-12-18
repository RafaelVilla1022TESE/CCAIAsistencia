using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using CCAIAsistencia.Data;
using CCAIAsistencia.Models;
using CCAIAsistencia.Utils;

namespace CCAIAsistencia.Views;

/// <summary>
/// Lista de usuarios con b£squeda, alta, edici¢n, eliminaci¢n y exportaci¢n contra base de datos.
/// </summary>
public class UserListView : Form
{
    private readonly AccessControlRepository _repository;
    private BindingList<User> _users;
    private BindingList<User> _filtered;

    private DataGridView _grid = null!;
    private TextBox _txtSearch = null!;
    private Button _btnSearch = null!;
    private Button _btnNew = null!;
    private Button _btnEdit = null!;
    private Button _btnDelete = null!;
    private Button _btnExport = null!;
    private Button _btnExportExcel = null!;

    public UserListView(AccessControlRepository repository)
    {
        _repository = repository;
        _users = new BindingList<User>(_repository.GetUsers());
        _filtered = new BindingList<User>(_users.ToList());
        InitializeComponent();
        ApplyFilter();
    }

    private void InitializeComponent()
    {
        Text = "Lista de usuarios";
        ClientSize = new Size(900, 480);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        Icon = new Icon(Path.Combine(AppContext.BaseDirectory, "Assets", "Icons", "users.ico"));

        var topPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 130
        };

        var lblSearch = new Label
        {
            Text = "Busqueda:",
            AutoSize = true,
            Location = new Point(20, 30)
        };

        _txtSearch = new TextBox
        {
            BackColor = SystemColors.Info,
            Location = new Point(110, 25),
            Width = 420
        };
        _txtSearch.KeyDown += (s, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                ApplyFilter();
            }
        };
        _txtSearch.TextChanged += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(_txtSearch.Text))
            {
                ApplyFilter();
            }
        };

        _btnSearch = new Button
        {
            Text = "Buscar",
            Width = 110,
            Location = new Point(550, 23)
        };
        _btnSearch.Click += (_, _) => ApplyFilter();

        AddCommonButtons();

        topPanel.Controls.Add(lblSearch);
        topPanel.Controls.Add(_txtSearch);
        topPanel.Controls.Add(_btnSearch);
        topPanel.Controls.Add(_btnNew);
        topPanel.Controls.Add(_btnEdit);
        topPanel.Controls.Add(_btnDelete);
        topPanel.Controls.Add(_btnExport);
        topPanel.Controls.Add(_btnExportExcel);

        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoGenerateColumns = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false
        };
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(User.Username), HeaderText = "Usuario", Width = 160 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(User.Email), HeaderText = "Email", Width = 220 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(User.Password), HeaderText = "Contrasena", Width = 160 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(User.Roles), HeaderText = "Roles", Width = 220 });
        _grid.CellFormatting += OnCellFormatting;

        Controls.Add(_grid);
        Controls.Add(topPanel);
    }

    private void AddCommonButtons()
    {
        _btnNew = new Button
        {
            Text = "Nuevo",
            Width = 100,
            Location = new Point(680, 10)
        };
        _btnNew.Click += (_, _) => OpenEditor(null);

        _btnEdit = new Button
        {
            Text = "Editar",
            Width = 100,
            Location = new Point(680, 40)
        };
        _btnEdit.Click += (_, _) =>
        {
            var selected = _grid.CurrentRow?.DataBoundItem as User;
            if (selected != null)
            {
                OpenEditor(selected);
            }
        };

        _btnDelete = new Button
        {
            Text = "Eliminar",
            Width = 100,
            Location = new Point(800, 10)
        };
        _btnDelete.Click += (_, _) => DeleteSelected();

        _btnExport = new Button
        {
            Text = "Exportar",
            Width = 100,
            Location = new Point(800, 40)
        };
        _btnExport.Click += (_, _) => ExportCsv();

        _btnExportExcel = new Button
        {
            Text = "Exportar Excel",
            Width = 120,
            Location = new Point(780, 74)
        };
        _btnExportExcel.Click += (_, _) => ExportExcel();
    }

    private void ApplyFilter()
    {
        var search = _txtSearch.Text?.Trim() ?? string.Empty;
        var query = _users.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(u =>
                (u.Username?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (u.Email?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        _filtered = new BindingList<User>(query.ToList());
        _grid.DataSource = _filtered;
    }

    private void OpenEditor(User? user)
    {
        using var form = new UserForm(_repository, user);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            ReloadUsers();
        }
        else
        {
            _grid.Refresh();
        }
    }

    private void DeleteSelected()
    {
        var selected = _grid.CurrentRow?.DataBoundItem as User;
        if (selected == null)
        {
            MessageBox.Show("Debes seleccionar un registro.", "Eliminar", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (selected.Username.Equals("admin", StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show("No se puede eliminar el usuario administrador.", "Eliminar", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var confirm = MessageBox.Show($"Eliminar registro {selected.Username}?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (confirm != DialogResult.Yes)
        {
            return;
        }

        _repository.DeleteUser(selected.Id);
        ReloadUsers();
    }

    private void ExportCsv()
    {
        if (_filtered.Count == 0)
        {
            MessageBox.Show("No hay datos para exportar.", "Exportar", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dialog = new SaveFileDialog
        {
            Filter = "CSV (*.csv)|*.csv",
            FileName = $"usuarios_{DateTime.Now:yyyyMMdd_HHmm}.csv"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine("Usuario,Email,Contrasena,Roles");
        foreach (var u in _filtered)
        {
            sb.AppendLine($"{Escape(u.Username)},{Escape(u.Email)},{Escape(u.Password)},{Escape(string.Join(";", u.Roles))}");
        }
        File.WriteAllText(dialog.FileName, sb.ToString(), Encoding.UTF8);
        MessageBox.Show("Archivo exportado correctamente.", "Exportar", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        if (value.Contains(',') || value.Contains('"'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }

    private void ExportExcel()
    {
        if (_filtered.Count == 0)
        {
            MessageBox.Show("No hay datos para exportar.", "Exportar", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        Export.GuardarArchivoExcel(_filtered.ToList(), "Usuarios");
    }

    private void OnCellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (_grid.Columns[e.ColumnIndex].DataPropertyName == nameof(User.Roles) && e.Value is IEnumerable<string> roles)
        {
            e.Value = string.Join(", ", roles);
            e.FormattingApplied = true;
        }
    }

    private void ReloadUsers()
    {
        _users = new BindingList<User>(_repository.GetUsers());
        ApplyFilter();
    }
}
