using System.ComponentModel;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AForge.Video;
using AForge.Video.DirectShow;
using CCAIAsistencia.Data;
using CCAIAsistencia.Models;
using CCAIAsistencia.Utils;

namespace CCAIAsistencia.Views;

/// <summary>
/// Formulario para crear o editar alumnos (antes empleados) persistidos en base de datos.
/// </summary>
public class EmployeeForm : Form
{
    private readonly AccessControlRepository _repository;
    private readonly Alumno? _employee;
    private readonly bool _isUpdate;
    private readonly ZktecoFingerprintService _fingerService;

    private TextBox _txtMatricula = null!;
    private TextBox _txtFirstName = null!;
    private TextBox _txtLastName = null!;
    private TextBox _txtEmail = null!;
    private TextBox _txtPhone = null!;
    private TextBox _txtAddress = null!;
    private ComboBox _cmbPerfil = null!;
    private TextBox _txtFingerprint = null!;
    private Button _btnCapture = null!;
    private ComboBox _cmbCameras = null!;
    private Button _btnStartCamera = null!;
    private Button _btnTakePhoto = null!;
    private PictureBox _picPreview = null!;
    private Label _lblFingerprintStatus = null!;
    private Label _lblPhotoStatus = null!;
    private Button _btnSave = null!;
    private Button _btnCancel = null!;

    private byte[]? _fingerprintBytes;
    private byte[]? _photoBytes;
    private FilterInfoCollection? _videoDevices;
    private VideoCaptureDevice? _videoSource;
    private readonly List<string> _perfiles = new();

    public EmployeeForm(AccessControlRepository repository, Alumno? employee)
    {
        _repository = repository;
        _employee = employee;
        _isUpdate = employee != null;
        _fingerService = new ZktecoFingerprintService();
        InitializeComponent();
        if (_isUpdate)
        {
            LoadData();
        }
        FormClosing += OnFormClosing;
    }

    private void InitializeComponent()
    {
        Text = _isUpdate ? "Editar alumno" : "Nuevo alumno";
        ClientSize = new Size(800, 720);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        AutoScroll = true;
        Icon = IconLoader.Load("student_add");

        var lblKey = new Label { Text = "Matricula:", Location = new Point(40, 35), AutoSize = true };
        _txtMatricula = new TextBox { BackColor = SystemColors.Info, Location = new Point(140, 30), Width = 360, MaxLength = 10 };

        var lblKeyHint = new Label { Text = "Ej. 1000,1001,1002", Location = new Point(140, 60), AutoSize = true, ForeColor = Color.DimGray, Font = new Font(DefaultFont.FontFamily, 8, FontStyle.Italic) };

        var lblFirst = new Label { Text = "Nombre:", Location = new Point(40, 95), AutoSize = true };
        _txtFirstName = new TextBox { BackColor = SystemColors.Info, Location = new Point(140, 90), Width = 360, MaxLength = 100 };

        var lblLast = new Label { Text = "Apellidos:", Location = new Point(40, 135), AutoSize = true };
        _txtLastName = new TextBox { BackColor = SystemColors.Info, Location = new Point(140, 130), Width = 360, MaxLength = 200 };

        var lblEmail = new Label { Text = "Email:", Location = new Point(40, 175), AutoSize = true };
        _txtEmail = new TextBox { BackColor = SystemColors.Info, Location = new Point(140, 170), Width = 360, MaxLength = 80 };

        var lblPhone = new Label { Text = "Telefono:", Location = new Point(40, 215), AutoSize = true };
        _txtPhone = new TextBox { BackColor = SystemColors.Info, Location = new Point(140, 210), Width = 360, MaxLength = 20 };

        var lblAddress = new Label { Text = "Direccion:", Location = new Point(40, 255), AutoSize = true };
        _txtAddress = new TextBox { BackColor = SystemColors.Info, Location = new Point(140, 250), Width = 360, MaxLength = 300 };

        var lblPerfil = new Label { Text = "Perfil:", Location = new Point(40, 295), AutoSize = true };
        _cmbPerfil = new ComboBox
        {
            BackColor = SystemColors.Info,
            Location = new Point(140, 290),
            Width = 360,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        LoadPerfiles();

        var lblFinger = new Label { Text = "Huella:", Location = new Point(40, 335), AutoSize = true };
        _txtFingerprint = new TextBox { BackColor = SystemColors.Info, Location = new Point(140, 330), Width = 180, ReadOnly = true };

        _btnCapture = new Button { Text = "Capturar", Location = new Point(340, 328), Width = 160 };
        _btnCapture.Click += (_, _) => CaptureFingerprint();
        _lblFingerprintStatus = new Label { Text = "Sin huella capturada.", Location = new Point(520, 332), AutoSize = true, ForeColor = Color.DimGray };

        // Seccion de foto
        var lblCamera = new Label { Text = "Camara:", Location = new Point(40, 375), AutoSize = true };
        _cmbCameras = new ComboBox { Location = new Point(140, 370), Width = 360, DropDownStyle = ComboBoxStyle.DropDownList };
        _btnStartCamera = new Button { Text = "Iniciar camara", Location = new Point(520, 368), Width = 140 };
        _btnStartCamera.Click += (_, _) => StartCamera();

        _btnTakePhoto = new Button { Text = "Tomar foto", Location = new Point(520, 404), Width = 140 };
        _btnTakePhoto.Click += (_, _) => CapturePhoto();

        _lblPhotoStatus = new Label { Text = "Foto opcional.", Location = new Point(40, 410), AutoSize = true, ForeColor = Color.DimGray };

        _picPreview = new PictureBox
        {
            Location = new Point(140, 410),
            Size = new Size(360, 160),
            BorderStyle = BorderStyle.FixedSingle,
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.Black
        };

        _btnSave = new Button { Text = "Guardar", Location = new Point(140, 600), Width = 120 };
        _btnSave.Click += (_, _) => SaveEmployee();

        _btnCancel = new Button { Text = "Cancelar", Location = new Point(280, 600), Width = 120 };
        _btnCancel.Click += (_, _) => DialogResult = DialogResult.Cancel;

        if (_cmbPerfil.Items.Count > 0 && _cmbPerfil.SelectedIndex < 0)
        {
            _cmbPerfil.SelectedIndex = 0;
        }

        Controls.Add(lblKey);
        Controls.Add(_txtMatricula);
        Controls.Add(lblKeyHint);
        Controls.Add(lblFirst);
        Controls.Add(_txtFirstName);
        Controls.Add(lblLast);
        Controls.Add(_txtLastName);
        Controls.Add(lblEmail);
        Controls.Add(_txtEmail);
        Controls.Add(lblPhone);
        Controls.Add(_txtPhone);
        Controls.Add(lblAddress);
        Controls.Add(_txtAddress);
        Controls.Add(lblPerfil);
        Controls.Add(_cmbPerfil);
        Controls.Add(lblFinger);
        Controls.Add(_txtFingerprint);
        Controls.Add(_btnCapture);
        Controls.Add(_lblFingerprintStatus);
        Controls.Add(lblCamera);
        Controls.Add(_cmbCameras);
        Controls.Add(_btnStartCamera);
        Controls.Add(_btnTakePhoto);
        Controls.Add(_lblPhotoStatus);
        Controls.Add(_picPreview);
        Controls.Add(_btnSave);
        Controls.Add(_btnCancel);

        LoadVideoDevices();
    }

    private void LoadPerfiles()
    {
        _perfiles.Clear();
        var dbPerfiles = _repository.GetPerfiles();
        if (dbPerfiles != null && dbPerfiles.Count > 0)
        {
            _perfiles.AddRange(dbPerfiles);
        }
        else
        {
            _perfiles.AddRange(new[]
            {
                "Servicio Social",
                "Residencia",
                "Dual",
                "Estancia",
                "Educación Media Superior",
                "Licenciatura",
                "Maestria",
                "Invitado",
                "Doctorado"
            });
        }

        _cmbPerfil.Items.Clear();
        _cmbPerfil.Items.AddRange(_perfiles.ToArray());
        if (_cmbPerfil.Items.Count > 0 && _cmbPerfil.SelectedIndex < 0)
        {
            _cmbPerfil.SelectedIndex = 0;
        }
    }

    private void LoadData()
    {
        _txtMatricula.Text = _employee!.Matricula.ToString();
        _txtMatricula.ReadOnly = true;
        _txtFirstName.Text = _employee.FirstName;
        _txtLastName.Text = _employee.LastName;
        _txtEmail.Text = _employee.Email;
        _txtPhone.Text = _employee.Phone;
        _txtAddress.Text = _employee.Address;
        if (!string.IsNullOrWhiteSpace(_employee.Perfil) && _perfiles.Contains(_employee.Perfil))
        {
            _cmbPerfil.SelectedItem = _employee.Perfil;
        }
        else if (_cmbPerfil.Items.Count > 0)
        {
            _cmbPerfil.SelectedIndex = 0;
        }
        _txtFingerprint.Text = _employee.Fingerprint != null ? "Huella registrada" : string.Empty;
        _fingerprintBytes = _employee.Fingerprint;
        _photoBytes = _employee.Photo;
        _lblFingerprintStatus.Text = _employee.Fingerprint != null ? "Huella cargada desde ficha." : "Sin huella capturada.";
        _lblFingerprintStatus.ForeColor = _employee.Fingerprint != null ? Color.ForestGreen : Color.DimGray;
        if (_photoBytes != null)
        {
            _picPreview.Image = ByteArrayToImage(_photoBytes);
            _lblPhotoStatus.Text = "Foto cargada.";
            _lblPhotoStatus.ForeColor = Color.ForestGreen;
        }
    }

    private void CaptureFingerprint()
    {
        using var wizard = new FingerprintCaptureForm(_fingerService);
        var result = wizard.ShowDialog(this);
        if (result == DialogResult.OK && wizard.Template != null)
        {
            _fingerprintBytes = wizard.Template;
            _txtFingerprint.Text = "Huella enrolada";
            _lblFingerprintStatus.Text = "Huella capturada correctamente.";
            _lblFingerprintStatus.ForeColor = Color.ForestGreen;
        }
        else
        {
            _lblFingerprintStatus.Text = "Huella no capturada.";
        _lblFingerprintStatus.ForeColor = Color.Firebrick;
    }
    }

    private void LoadVideoDevices()
    {
        _videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
        _cmbCameras.Items.Clear();
        foreach (FilterInfo device in _videoDevices)
        {
            _cmbCameras.Items.Add(device.Name);
        }

        if (_cmbCameras.Items.Count > 0)
        {
            _cmbCameras.SelectedIndex = 0;
        }
        else
        {
            _cmbCameras.Items.Add("No hay camaras disponibles");
            _cmbCameras.SelectedIndex = 0;
            _cmbCameras.Enabled = false;
            _btnStartCamera.Enabled = false;
            _btnTakePhoto.Enabled = false;
            _lblPhotoStatus.Text = "No se detectan camaras.";
        }
    }

    private void StartCamera()
    {
        if (_videoDevices == null || _videoDevices.Count == 0)
        {
            MessageBox.Show("No se encontraron camaras.", "Camara", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        StopCamera();

        var device = _videoDevices[_cmbCameras.SelectedIndex];
        _videoSource = new VideoCaptureDevice(device.MonikerString);
        _videoSource.NewFrame += OnNewFrame;
        _videoSource.Start();
        _lblPhotoStatus.Text = "Camara activa. Presiona Tomar foto.";
        _lblPhotoStatus.ForeColor = Color.DarkOrange;
    }

    private void OnNewFrame(object sender, NewFrameEventArgs eventArgs)
    {
        var bitmap = (Bitmap)eventArgs.Frame.Clone();
        _picPreview.Invoke(new Action(() =>
        {
            _picPreview.Image?.Dispose();
            _picPreview.Image = bitmap;
        }));
    }

    private void CapturePhoto()
    {
        if (_picPreview.Image == null)
        {
            MessageBox.Show("Inicia la camara y espera a ver la imagen para capturar.", "Camara", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var ms = new MemoryStream();
        _picPreview.Image.Save(ms, ImageFormat.Jpeg);
        _photoBytes = ms.ToArray();
        _lblPhotoStatus.Text = "Foto capturada.";
        _lblPhotoStatus.ForeColor = Color.ForestGreen;
    }

    private void StopCamera()
    {
        if (_videoSource != null)
        {
            _videoSource.NewFrame -= OnNewFrame;
            if (_videoSource.IsRunning)
            {
                _videoSource.SignalToStop();
                var start = Environment.TickCount;
                // Evitar bloqueo prolongado en UI al cerrar la ventana.
                while (_videoSource.IsRunning && Environment.TickCount - start < 800)
                {
                    Application.DoEvents();
                    Thread.Sleep(20);
                }
                if (_videoSource.IsRunning)
                {
                    _videoSource.Stop();
                }
            }
            _videoSource = null;
        }
    }

    private void SaveEmployee()
    {
        // Detener camara antes de operaciones de guardado para evitar bloqueos en UI.
        StopCamera();

        if (!ValidateInputs(out var matricula))
        {
            return;
        }

        if (_repository.ExistsMatricula(matricula, _employee?.Id))
        {
            MessageBox.Show("Ya existe otro alumno con la misma matricula.", "Validacion", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (_isUpdate)
        {
            _employee!.Matricula = matricula;
            _employee.FirstName = _txtFirstName.Text.Trim();
            _employee.LastName = _txtLastName.Text.Trim();
            _employee.Email = _txtEmail.Text.Trim();
            _employee.Phone = _txtPhone.Text.Trim();
            _employee.Address = _txtAddress.Text.Trim();
            _employee.Perfil = _cmbPerfil.SelectedItem?.ToString() ?? string.Empty;
            _employee.Fingerprint = _fingerprintBytes;
            _employee.Photo = _photoBytes;
            _repository.UpdateAlumno(_employee);
        }
        else
        {
            var nuevo = new Alumno
            {
                Matricula = matricula,
                Perfil = _cmbPerfil.SelectedItem?.ToString() ?? string.Empty,
                FirstName = _txtFirstName.Text.Trim(),
                LastName = _txtLastName.Text.Trim(),
                Email = _txtEmail.Text.Trim(),
                Phone = _txtPhone.Text.Trim(),
                Address = _txtAddress.Text.Trim(),
                RegistrationDate = DateTime.Now,
                IsActive = true,
                Fingerprint = _fingerprintBytes,
                Photo = _photoBytes
            };
            _repository.CreateAlumno(nuevo);
        }

        DialogResult = DialogResult.OK;
    }

    private bool ValidateInputs(out int matricula)
    {
        matricula = 0;
        if (!int.TryParse(_txtMatricula.Text.Trim(), out matricula) || matricula <= 0)
        {
            MessageBox.Show("Ingresa una matricula numerica valida (ej. 1000).", "Validacion", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        if (string.IsNullOrWhiteSpace(_txtFirstName.Text))
        {
            MessageBox.Show("Ingresa un nombre.", "Validacion", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        if (string.IsNullOrWhiteSpace(_txtLastName.Text))
        {
            MessageBox.Show("Ingresa apellidos.", "Validacion", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        if (_cmbPerfil.SelectedItem == null)
        {
            MessageBox.Show("Selecciona un perfil.", "Validacion", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        if (_fingerprintBytes == null)
        {
            var confirm = MessageBox.Show("No se ha capturado la huella. Deseas guardar sin huella?", "Huella no capturada", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes)
            {
                return false;
            }
        }

        return true;
    }

    private Image ByteArrayToImage(byte[] bytes)
    {
        using var ms = new MemoryStream(bytes);
        return Image.FromStream(ms);
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        StopCamera();
        _fingerService.Dispose();
    }
}
