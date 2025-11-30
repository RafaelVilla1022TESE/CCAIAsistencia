using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using CCAIAsistencia.Models;
using CCAIAsistencia.Utils;

namespace CCAIAsistencia.Views;

/// <summary>
/// Reporte de asistencias con filtros por fecha y texto, y exportacion a CSV.
/// </summary>
public class ReportsForm : Form
{
    private readonly BindingList<ReportItem> _data;
    private BindingList<ReportItem> _filtered;

    private DataGridView _grid = null!;
    private Button _btnFilter = null!;
    private Button _btnExportCsv = null!;
    private Button _btnExportExcel = null!;
    private TextBox _txtSearch = null!;
    private Label _lblSearch = null!;
    private DateTimePicker _dtpStart = null!;
    private DateTimePicker _dtpEnd = null!;

    public ReportsForm(BindingList<ReportItem> data)
    {
        _data = data;
        _filtered = new BindingList<ReportItem>(_data.ToList());
        InitializeComponent();
        Load += OnLoad;
    }

    private void InitializeComponent()
    {
        Text = "Reportes";
        ClientSize = new Size(1000, 680);
        MinimumSize = new Size(900, 600);

        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoGenerateColumns = false,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeColumns = false,
            AllowUserToResizeRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells
        };
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(ReportItem.Timestamp), HeaderText = "Fecha/Hora", Width = 180 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(ReportItem.Type), HeaderText = "Tipo", Width = 90 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(ReportItem.FirstName), HeaderText = "Nombre", Width = 140 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(ReportItem.LastName), HeaderText = "Apellidos", Width = 180 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(ReportItem.Phone), HeaderText = "Telefono", Width = 120 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(ReportItem.Email), HeaderText = "Email", Width = 200 });
        _grid.CellFormatting += (_, e) =>
        {
            if (_grid.Columns[e.ColumnIndex].DataPropertyName == nameof(ReportItem.Timestamp) && e.Value is DateTime dt)
            {
                e.Value = dt.ToString("g");
                e.FormattingApplied = true;
            }
        };

        _lblSearch = new Label
        {
            Text = "Buscar por:",
            AutoSize = true,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(0, 5, 0, 0)
        };
        _txtSearch = new TextBox { PlaceholderText = "Nombre, apellidos, email o telefono", Width = 260, BackColor = SystemColors.Info };
        _dtpStart = new DateTimePicker { Value = DateTime.Now.AddDays(-1) };
        _dtpEnd = new DateTimePicker { Value = DateTime.Now };

        _btnFilter = new Button { Text = "Filtrar", Width = 100 };
        _btnFilter.Click += (_, _) => ApplyFilter();

        _btnExportCsv = new Button { Text = "Exportar CSV", Width = 120 };
        _btnExportCsv.Click += (_, _) => ExportCsv();
        _btnExportExcel = new Button { Text = "Exportar Excel", Width = 120 };
        _btnExportExcel.Click += (_, _) => ExportExcel();

        var topPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 140,
            Padding = new Padding(12)
        };

        var lblStart = new Label
        {
            Text = "Fecha inicio",
            AutoSize = true,
            Location = new Point(10, 12)
        };
        _dtpStart.Location = new Point(10, 40);

        var lblEnd = new Label
        {
            Text = "Fecha fin",
            AutoSize = true,
            Location = new Point(250, 12)
        };
        _dtpEnd.Location = new Point(250, 40);

        _lblSearch.Location = new Point(10, 80);
        _txtSearch.Location = new Point(100, 78);
        _btnFilter.Location = new Point(380, 76);
        _btnExportCsv.Location = new Point(500, 76);
        _btnExportExcel.Location = new Point(630, 76);

        topPanel.Controls.Add(lblStart);
        topPanel.Controls.Add(_dtpStart);
        topPanel.Controls.Add(lblEnd);
        topPanel.Controls.Add(_dtpEnd);
        topPanel.Controls.Add(_lblSearch);
        topPanel.Controls.Add(_txtSearch);
        topPanel.Controls.Add(_btnFilter);
        topPanel.Controls.Add(_btnExportCsv);
        topPanel.Controls.Add(_btnExportExcel);

        Controls.Add(_grid);
        Controls.Add(topPanel);
    }

    private void OnLoad(object? sender, EventArgs e)
    {
        SetDefaultDates();
        ApplyFilter();
    }

    private void SetDefaultDates()
    {
        _dtpStart.Value = DateTime.Now.AddDays(-1);
        _dtpEnd.Value = DateTime.Now;
    }

    private void ApplyFilter()
    {
        var search = _txtSearch.Text?.Trim() ?? string.Empty;
        var start = _dtpStart.Value.Date;
        var end = _dtpEnd.Value.Date.AddDays(1).AddTicks(-1);

        var query = _data.Where(x => x.Timestamp >= start && x.Timestamp <= end);
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x =>
                (x.FirstName?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (x.LastName?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (x.Email?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (x.Phone?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        _filtered = new BindingList<ReportItem>(query.ToList());
        _grid.DataSource = _filtered;
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
            FileName = $"reporte_{DateTime.Now:yyyyMMdd_HHmm}.csv"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine("FechaHora,Tipo,Nombre,Apellidos,Telefono,Email");
        foreach (var item in _filtered)
        {
            sb.AppendLine($"{item.Timestamp:yyyy-MM-dd HH:mm},{Escape(item.Type)},{Escape(item.FirstName)},{Escape(item.LastName)},{Escape(item.Phone)},{Escape(item.Email)}");
        }

        File.WriteAllText(dialog.FileName, sb.ToString(), Encoding.UTF8);
        MessageBox.Show("Archivo exportado correctamente.", "Exportar", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void ExportExcel()
    {
        if (_filtered.Count == 0)
        {
            MessageBox.Show("No hay datos para exportar.", "Exportar", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        Export.GuardarArchivoExcel(_filtered.ToList(), "Reporte");
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
}
