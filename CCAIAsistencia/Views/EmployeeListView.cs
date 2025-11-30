using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using CCAIAsistencia.Models;
using CCAIAsistencia.Utils;

namespace CCAIAsistencia.Views;

/// <summary>
/// Lista de empleados con búsqueda, alta/edición, eliminación y exportación en memoria.
/// </summary>
public class AlumnoListView : Form
{
    private readonly BindingList<Alumno> _employees;
    private BindingList<Alumno> _filtered;

    private DataGridView _grid = null!;
    private TextBox _txtSearch = null!;
    private Button _btnSearch = null!;
    private Button _btnNew = null!;
    private Button _btnEdit = null!;
    private Button _btnDelete = null!;
    private Button _btnExport = null!;
    private Button _btnExportExcel = null!;

    public AlumnoListView(BindingList<Alumno> employees)
    {
        _employees = employees;
        _filtered = new BindingList<Alumno>(_employees.ToList());
        InitializeComponent();
        ApplyFilter();
    }

    private void InitializeComponent()
    {
        Text = "Alumnos";
        ClientSize = new Size(900, 480);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;

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

        AddButtons();

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
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Alumno.UserKey), HeaderText = "Matricula", Width = 80 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Alumno.FirstName), HeaderText = "Nombre", Width = 160 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Alumno.LastName), HeaderText = "Apellidos", Width = 180 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Alumno.Email), HeaderText = "Email", Width = 200 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Alumno.Phone), HeaderText = "Telefono", Width = 120 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Alumno.Address), HeaderText = "Direccion", Width = 200 });
        _grid.CellFormatting += (_, e) =>
        {
            if (_grid.Columns[e.ColumnIndex].DataPropertyName == nameof(Alumno.RegistrationDate) && e.Value is DateTime dt)
            {
                e.Value = dt.ToString("d");
                e.FormattingApplied = true;
            }
        };
        _grid.CellDoubleClick += (_, _) => OpenEditor(_grid.CurrentRow?.DataBoundItem as Alumno);

        Controls.Add(_grid);
        Controls.Add(topPanel);
    }

    private void AddButtons()
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
            var selected = _grid.CurrentRow?.DataBoundItem as Alumno;
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
        var query = _employees.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(e =>
                e.UserKey.ToString().Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (e.FirstName?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (e.LastName?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (e.Email?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (e.Phone?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (e.Address?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        _filtered = new BindingList<Alumno>(query.ToList());
        _grid.DataSource = _filtered;
    }

    private void OpenEditor(Alumno? employee)
    {
        using var form = new EmployeeForm(_employees, employee);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            ApplyFilter();
        }
        else
        {
            _grid.Refresh();
        }
    }

    private void DeleteSelected()
    {
        var selected = _grid.CurrentRow?.DataBoundItem as Alumno;
        if (selected == null)
        {
            MessageBox.Show("Debes seleccionar un registro.", "Eliminar", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var confirm = MessageBox.Show($"Eliminar registro {selected.FirstName} {selected.LastName}?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (confirm != DialogResult.Yes)
        {
            return;
        }

        _employees.Remove(selected);
        ApplyFilter();
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
            FileName = $"alumnos_{DateTime.Now:yyyyMMdd_HHmm}.csv"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine("Matricula,Nombre,Apellidos,Email,Telefono,Direccion");
        foreach (var e in _filtered)
        {
            sb.AppendLine($"{e.UserKey},{Escape(e.FirstName)},{Escape(e.LastName)},{Escape(e.Email)},{Escape(e.Phone)},{Escape(e.Address)}");
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

        Export.GuardarArchivoExcel(_filtered.ToList(), "Alumnos");
    }
}
