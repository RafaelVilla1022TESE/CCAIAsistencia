using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Media;
using CCAIAsistencia.Models;

namespace CCAIAsistencia.Views;

/// <summary>
/// Checador simple en memoria. Busca por clave y agrega un registro a la lista de reportes.
/// </summary>
public class CheckInForm : Form
{
    private readonly BindingList<Alumno> _employees;
    private readonly BindingList<ReportItem> _reports;

    private TextBox _txtUserKey = null!;
    private Label _lblName = null!;
    private Label _lblStatus = null!;
    private PictureBox _picPhoto = null!;
    private Button _btnRegister = null!;

    public CheckInForm(BindingList<Alumno> employees, BindingList<ReportItem> reports)
    {
        _employees = employees;
        _reports = reports;
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        Text = "Checador";
        ClientSize = new Size(620, 280);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        var lblKey = new Label { Text = "Clave:", Location = new Point(40, 40), AutoSize = true, Font = new Font(DefaultFont, FontStyle.Bold) };
        _txtUserKey = new TextBox
        {
            BackColor = SystemColors.Info,
            Location = new Point(110, 36),
            Width = 200,
            Font = new Font(DefaultFont.FontFamily, 14, FontStyle.Bold)
        };
        _txtUserKey.KeyPress += (s, e) =>
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        };
        _txtUserKey.KeyDown += (s, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                Register();
            }
        };

        _btnRegister = new Button
        {
            Text = "Registrar",
            Location = new Point(330, 34),
            Width = 120,
            Height = 36
        };
        _btnRegister.Click += (_, _) => Register();

        var lblEmpleado = new Label { Text = "Empleado:", Location = new Point(40, 110), AutoSize = true };
        _lblName = new Label { Text = "-", Location = new Point(130, 110), AutoSize = true, Font = new Font(DefaultFont, FontStyle.Bold) };

        _lblStatus = new Label
        {
            Text = string.Empty,
            Location = new Point(40, 160),
            AutoSize = true,
            ForeColor = Color.Firebrick,
            Font = new Font(DefaultFont.FontFamily, 10f, FontStyle.Bold)
        };

        _picPhoto = new PictureBox
        {
            Location = new Point(420, 90),
            Size = new Size(160, 160),
            BorderStyle = BorderStyle.FixedSingle,
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.Black
        };

        Controls.Add(lblKey);
        Controls.Add(_txtUserKey);
        Controls.Add(_btnRegister);
        Controls.Add(lblEmpleado);
        Controls.Add(_lblName);
        Controls.Add(_lblStatus);
        Controls.Add(_picPhoto);
    }

    private void Register()
    {
        var value = _txtUserKey.Text.Trim();
        if (!int.TryParse(value, out var key) || key <= 0)
        {
            SetStatus("Ingresa una clave numerica valida.", success: false);
            return;
        }

        var employee = _employees.FirstOrDefault(e => e.UserKey == key);
        if (employee == null)
        {
            SetStatus("Registro no encontrado.", success: false);
            _lblName.Text = "-";
            _picPhoto.Image = null;
            SystemSounds.Hand.Play();
            return;
        }

        _lblName.Text = $"{employee.FirstName} {employee.LastName}".Trim();
        SetPhoto(employee.Photo);
        _reports.Add(new ReportItem
        {
            Timestamp = DateTime.Now,
            Type = "Checador",
            FirstName = employee.FirstName,
            LastName = employee.LastName,
            Phone = employee.Phone,
            Email = employee.Email
        });
        SetStatus("Registro correcto.", success: true);
        SystemSounds.Asterisk.Play();
    }

    private void SetStatus(string message, bool success)
    {
        _lblStatus.Text = message;
        _lblStatus.ForeColor = success ? Color.ForestGreen : Color.Firebrick;
    }

    private void SetPhoto(byte[]? photoBytes)
    {
        _picPhoto.Image = null;
        if (photoBytes == null)
        {
            return;
        }

        using var ms = new MemoryStream(photoBytes);
        _picPhoto.Image = Image.FromStream(ms);
    }
}
