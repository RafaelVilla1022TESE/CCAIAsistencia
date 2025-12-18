using System.Drawing;
using System.Linq;
using System.Media;
using System.Threading;
using System.Threading.Tasks;
using System;
using CCAIAsistencia.Data;
using CCAIAsistencia.Models;
using CCAIAsistencia.Utils;

namespace CCAIAsistencia.Views;

/// <summary>
/// Checador simple contra base de datos. Busca por matricula o por huella y registra asistencia.
/// </summary>
public class CheckInForm : Form
{
    private readonly AccessControlRepository _repository;
    private List<Alumno> _students;
    private readonly ZktecoFingerprintService _fingerService;
    private readonly CancellationTokenSource _cts = new();
    private CancellationTokenSource _clearFieldsCts = new();

    private TextBox _txtMatricula = null!;
    private Label _lblName = null!;
    private Label _lblStatus = null!;
    private PictureBox _picPhoto = null!;
    private Button _btnRegister = null!;
    private Button _btnScanFingerprint = null!;
    private Label _lblFingerStatus = null!;

    private bool _listening;

    public CheckInForm(AccessControlRepository repository)
    {
        _repository = repository;
        _fingerService = new ZktecoFingerprintService();
        _students = _repository.GetAlumnos();
        InitializeComponent();
        FormClosing += OnFormClosing;
        Shown += OnShown;
    }

    private void InitializeComponent()
    {
        Text = "Checador";
        ClientSize = new Size(620, 280);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Icon = IconLoader.Load("checkin");

        var lblKey = new Label { Text = "Matricula:", Location = new Point(40, 40), AutoSize = true, Font = new Font(DefaultFont, FontStyle.Bold) };
        _txtMatricula = new TextBox
        {
            BackColor = SystemColors.ControlLight,
            Location = new Point(110, 36),
            Width = 200,
            Font = new Font(DefaultFont.FontFamily, 14, FontStyle.Bold),
            ReadOnly = true,
            Enabled = false
        };
        _txtMatricula.KeyPress += (s, e) =>
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        };
        _txtMatricula.KeyDown += (s, e) =>
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
        _btnRegister.Enabled = false;

        _btnScanFingerprint = new Button
        {
            Text = "Escanear huella",
            Location = new Point(470, 34),
            Width = 130,
            Height = 36
        };
        _btnScanFingerprint.Visible = false;

        var lblAlumno = new Label { Text = "Alumno:", Location = new Point(40, 110), AutoSize = true };
        _lblName = new Label { Text = "-", Location = new Point(130, 110), AutoSize = true, Font = new Font(DefaultFont, FontStyle.Bold) };

        _lblStatus = new Label
        {
            Text = string.Empty,
            Location = new Point(40, 160),
            AutoSize = true,
            ForeColor = Color.Firebrick,
            Font = new Font(DefaultFont.FontFamily, 10f, FontStyle.Bold)
        };

        _lblFingerStatus = new Label
        {
            Text = "Listo para leer huella con ZK9500.",
            Location = new Point(40, 190),
            AutoSize = true,
            ForeColor = Color.DimGray
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
        Controls.Add(_txtMatricula);
        Controls.Add(_btnRegister);
        Controls.Add(_btnScanFingerprint);
        Controls.Add(lblAlumno);
        Controls.Add(_lblName);
        Controls.Add(_lblStatus);
        Controls.Add(_lblFingerStatus);
        Controls.Add(_picPhoto);
    }

    private void Register()
    {
        var value = _txtMatricula.Text.Trim();
        if (!int.TryParse(value, out var key) || key <= 0)
        {
            SetStatus("Ingresa una matricula numerica valida.", success: false);
            ScheduleClearFields();
            return;
        }

        var employee = _repository.GetAlumnoByMatricula(key);
        if (employee == null)
        {
            SetStatus("Registro no encontrado.", success: false);
            SetFingerprintStatus("No hay coincidencias.", success: false);
            _lblName.Text = "-";
            _picPhoto.Image = null;
            PlayFailureSound();
            ScheduleClearFields();
            return;
        }

        CompleteRegistration(employee);
    }

    private async Task RegisterWithFingerprintAsync()
    {
        ToggleFingerprintUi(false);
        _lblFingerStatus.Text = "Coloca el dedo en el lector...";
        _lblFingerStatus.ForeColor = Color.DarkOrange;

        try
        {
            _students = _repository.GetAlumnos();
            var snapshot = _students.ToList();

            var result = await Task.Run(() =>
            {
                var ok = _fingerService.TryIdentifyStudentByMatch(snapshot, out var match, out var msg,
                    timeoutMs: 15000, minScore: 60, cancellationToken: _cts.Token);
                return (ok, match, msg);
            });

            SetFingerprintStatus(result.msg, result.ok);

            if (!result.ok || result.match == null)
            {
                SetStatus(result.msg, success: false);
                ClearIdentityFields();
                PlayFailureSound();
                ScheduleClearFields();
                return;
            }

            _txtMatricula.Text = result.match.Matricula.ToString();
            CompleteRegistration(result.match);
        }
        catch (Exception ex)
        {
            SetStatus($"Error al leer huella: {ex.Message}", success: false);
            SetFingerprintStatus("Error con el lector.", success: false);
            ClearIdentityFields();
            ScheduleClearFields();
        }
        finally
        {
            ToggleFingerprintUi(true);
        }
    }

    private void CompleteRegistration(Alumno employee)
    {
        _lblName.Text = $"{employee.FirstName} {employee.LastName}".Trim();
        SetPhoto(employee.Photo);
        if (employee.Id <= 0)
        {
            SetStatus("El alumno no tiene Id valido en BD.", success: false);
            return;
        }

        _repository.AddAttendanceRecord(employee.Id, "Checador");
        SetStatus("Registro correcto.", success: true);
        SetFingerprintStatus("Huella validada.", success: true);
        PlaySuccessSound();
        ScheduleClearFields();
    }

    private void SetStatus(string message, bool success)
    {
        _lblStatus.Text = message;
        _lblStatus.ForeColor = success ? Color.ForestGreen : Color.Firebrick;
    }

    private void SetFingerprintStatus(string message, bool success)
    {
        _lblFingerStatus.Text = message;
        _lblFingerStatus.ForeColor = success ? Color.ForestGreen : Color.Firebrick;
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

    private void ToggleFingerprintUi(bool enabled)
    {
        _btnScanFingerprint.Enabled = enabled;
        _btnRegister.Enabled = enabled;
        _txtMatricula.Enabled = enabled;
        Cursor = enabled ? Cursors.Default : Cursors.WaitCursor;
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        _listening = false;
        _cts.Cancel();
        _fingerService.Dispose();
        _cts.Dispose();
        _clearFieldsCts.Cancel();
        _clearFieldsCts.Dispose();
    }

    private void OnShown(object? sender, EventArgs e)
    {
        _listening = true;
        _ = ListenFingerprintsAsync();
    }

    private async Task ListenFingerprintsAsync()
    {
        while (_listening && !_cts.Token.IsCancellationRequested)
        {
            try
            {
                _students = _repository.GetAlumnos();
                var snapshot = _students.ToList();

                var result = await Task.Run(() =>
                {
                    var ok = _fingerService.TryIdentifyStudentByMatch(snapshot, out var match, out var msg,
                        timeoutMs: 15000, minScore: 60, cancellationToken: _cts.Token);
                    return (ok, match, msg);
                });

                SetFingerprintStatus(result.msg, result.ok);

                if (result.ok && result.match != null)
                {
                    _txtMatricula.Text = result.match.Matricula.ToString();
                    CompleteRegistration(result.match);
                }
                else
                {
                    SetStatus(result.msg, success: false);
                    ClearIdentityFields();
                    PlayFailureSound();
                    ScheduleClearFields();
                }
            }
            catch (Exception ex)
            {
                SetStatus($"Error al leer huella: {ex.Message}", success: false);
                SetFingerprintStatus("Error con el lector.", success: false);
                ClearIdentityFields();
                PlayFailureSound();
                ScheduleClearFields();
            }
            finally
            {
                await Task.Delay(500);
            }
        }
    }

    private void ScheduleClearFields()
    {
        _clearFieldsCts.Cancel();
        _clearFieldsCts.Dispose();
        _clearFieldsCts = new CancellationTokenSource();

        var token = _clearFieldsCts.Token;
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(5), token);
                if (token.IsCancellationRequested || IsDisposed || !IsHandleCreated)
                {
                    return;
                }

                BeginInvoke((Action)ResetCheckInFields);
            }
            catch (TaskCanceledException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
        }, token);
    }

    private void ResetCheckInFields()
    {
        ClearIdentityFields();
        _lblStatus.Text = string.Empty;
        _lblFingerStatus.Text = "Listo para leer huella con ZK9500.";
        _lblFingerStatus.ForeColor = Color.DimGray;
    }

    private void ClearIdentityFields()
    {
        _txtMatricula.Text = string.Empty;
        _lblName.Text = "-";
        _picPhoto.Image = null;
    }

    private void PlaySuccessSound()
    {
        try
        {
            Console.Beep(1200, 200);
            Console.Beep(1500, 200);
        }
        catch
        {
            SystemSounds.Asterisk.Play();
        }
    }

    private void PlayFailureSound()
    {
        try
        {
            Console.Beep(400, 250);
            Console.Beep(300, 300);
        }
        catch
        {
            SystemSounds.Hand.Play();
        }
    }
}
