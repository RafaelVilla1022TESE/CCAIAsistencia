using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using AForge.Video;
using AForge.Video.DirectShow;
using CCAIAsistencia.Models;

namespace CCAIAsistencia.Views;

/// <summary>
/// Formulario para crear o editar empleados en memoria.
/// </summary>
public class EmployeeForm : Form
{
    public delegate void EmployeeSavedEventHandler();

    private readonly BindingList<Alumno> _employees;
    private readonly Alumno? _employee;
    private readonly bool _isUpdate;

    private TextBox _txtUserKey = null!;
    private TextBox _txtFirstName = null!;
    private TextBox _txtLastName = null!;
    private TextBox _txtEmail = null!;
    private TextBox _txtPhone = null!;
    private TextBox _txtAddress = null!;
    private TextBox _txtFingerprint = null!;
    private Button _btnCapture = null!;
    private ComboBox _cmbCameras = null!;
    private Button _btnStartCamera = null!;
    private Button _btnTakePhoto = null!;
    private PictureBox _picPreview = null!;
    private Label _lblPhotoStatus = null!;
    private Button _btnSave = null!;
    private Button _btnCancel = null!;

    private byte[]? _fingerprintBytes;
    private byte[]? _photoBytes;
    private FilterInfoCollection? _videoDevices;
    private VideoCaptureDevice? _videoSource;

    public event EmployeeSavedEventHandler? EmployeeSaved;

    public EmployeeForm(BindingList<Alumno> employees, Alumno? employee)
    {
        _employees = employees;
        _employee = employee;
        _isUpdate = employee != null;
        InitializeComponent();
        if (_isUpdate)
        {
            LoadData();
        }
        FormClosing += OnFormClosing;
    }

    private void InitializeComponent()
    {
        Text = _isUpdate ? "Editar empleado" : "Nuevo empleado";
        ClientSize = new Size(800, 600);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        var lblKey = new Label { Text = "Clave:", Location = new Point(40, 35), AutoSize = true };
        _txtUserKey = new TextBox { BackColor = SystemColors.Info, Location = new Point(140, 30), Width = 360, MaxLength = 10 };

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

        var lblFinger = new Label { Text = "Huella:", Location = new Point(40, 295), AutoSize = true };
        _txtFingerprint = new TextBox { BackColor = SystemColors.Info, Location = new Point(140, 290), Width = 180, ReadOnly = true };

        _btnCapture = new Button { Text = "Capturar", Location = new Point(340, 288), Width = 160 };
        _btnCapture.Click += (_, _) => CaptureFingerprint();

        // Seccion de foto
        var lblCamera = new Label { Text = "Camara:", Location = new Point(40, 335), AutoSize = true };
        _cmbCameras = new ComboBox { Location = new Point(140, 330), Width = 360, DropDownStyle = ComboBoxStyle.DropDownList };
        _btnStartCamera = new Button { Text = "Iniciar camara", Location = new Point(520, 328), Width = 140 };
        _btnStartCamera.Click += (_, _) => StartCamera();

        _btnTakePhoto = new Button { Text = "Tomar foto", Location = new Point(520, 364), Width = 140 };
        _btnTakePhoto.Click += (_, _) => CapturePhoto();

        _lblPhotoStatus = new Label { Text = "Foto requerida para registrar.", Location = new Point(40, 370), AutoSize = true, ForeColor = Color.Firebrick };

        _picPreview = new PictureBox
        {
            Location = new Point(140, 370),
            Size = new Size(360, 200),
            BorderStyle = BorderStyle.FixedSingle,
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.Black
        };

        _btnSave = new Button { Text = "Guardar", Location = new Point(140, 580), Width = 120 };
        _btnSave.Click += (_, _) => SaveEmployee();

        _btnCancel = new Button { Text = "Cancelar", Location = new Point(280, 580), Width = 120 };
        _btnCancel.Click += (_, _) => DialogResult = DialogResult.Cancel;

        Controls.Add(lblKey);
        Controls.Add(_txtUserKey);
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
        Controls.Add(lblFinger);
        Controls.Add(_txtFingerprint);
        Controls.Add(_btnCapture);
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

    private void LoadData()
    {
        _txtUserKey.Text = _employee!.UserKey.ToString();
        _txtUserKey.ReadOnly = true;
        _txtFirstName.Text = _employee.FirstName;
        _txtLastName.Text = _employee.LastName;
        _txtEmail.Text = _employee.Email;
        _txtPhone.Text = _employee.Phone;
        _txtAddress.Text = _employee.Address;
        _txtFingerprint.Text = _employee.Fingerprint != null ? "Huella registrada" : string.Empty;
        _fingerprintBytes = _employee.Fingerprint;
        _photoBytes = _employee.Photo;
        if (_photoBytes != null)
        {
            _picPreview.Image = ByteArrayToImage(_photoBytes);
            _lblPhotoStatus.Text = "Foto cargada.";
            _lblPhotoStatus.ForeColor = Color.ForestGreen;
        }
    }

    private void CaptureFingerprint()
    {
        // Simulacion de captura de huella (sin dependencia a DPFP).
        _fingerprintBytes = Guid.NewGuid().ToByteArray();
        _txtFingerprint.Text = "Huella capturada correctamente";
        MessageBox.Show("Enrolamiento exitoso (simulado).", "Huella", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                _videoSource.WaitForStop();
            }
            _videoSource = null;
        }
    }

    private void SaveEmployee()
    {
        if (!ValidateInputs(out var userKey))
        {
            return;
        }

        if (_isUpdate)
        {
            var existsOther = _employees.Any(e => e != _employee && e.UserKey == userKey);
            if (existsOther)
            {
                MessageBox.Show("Ya existe otro empleado con la misma clave.", "Validacion", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _employee!.UserKey = userKey;
            _employee.FirstName = _txtFirstName.Text.Trim();
            _employee.LastName = _txtLastName.Text.Trim();
            _employee.Email = _txtEmail.Text.Trim();
            _employee.Phone = _txtPhone.Text.Trim();
            _employee.Address = _txtAddress.Text.Trim();
            _employee.Fingerprint = _fingerprintBytes;
            _employee.Photo = _photoBytes;
        }
        else
        {
            if (_employees.Any(e => e.UserKey == userKey))
            {
                MessageBox.Show("Ya existe un empleado con esa clave.", "Validacion", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

        _employees.Add(new Alumno
            {
                UserKey = userKey,
                FirstName = _txtFirstName.Text.Trim(),
                LastName = _txtLastName.Text.Trim(),
                Email = _txtEmail.Text.Trim(),
                Phone = _txtPhone.Text.Trim(),
                Address = _txtAddress.Text.Trim(),
                RegistrationDate = DateTime.Now,
                IsActive = true,
                Fingerprint = _fingerprintBytes,
                Photo = _photoBytes
            });
        }

        EmployeeSaved?.Invoke();
        DialogResult = DialogResult.OK;
    }

    private bool ValidateInputs(out int userKey)
    {
        userKey = 0;
        if (!int.TryParse(_txtUserKey.Text.Trim(), out userKey) || userKey <= 0)
        {
            MessageBox.Show("Ingresa una clave numerica valida (ej. 1000).", "Validacion", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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

        if (_photoBytes == null)
        {
            MessageBox.Show("Debes tomar una foto para registrar al empleado.", "Foto requerida", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        if (string.IsNullOrWhiteSpace(_txtFingerprint.Text))
        {
            var confirm = MessageBox.Show("No se ha capturado la huella. ¿Deseas guardar sin huella?", "Huella no capturada", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
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
    }
}
