using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using CCAIAsistencia.Utils;

namespace CCAIAsistencia.Views;

/// <summary>
/// Modal sencillo que muestra progreso de enrolamiento de huella (3 lecturas).
/// </summary>
public class FingerprintCaptureForm : Form
{
    private readonly ZktecoFingerprintService _fingerService;
    private Label _lblStatus = null!;
    private ProgressBar _progress = null!;
    private Button _btnClose = null!;

    public byte[]? Template { get; private set; }

    private readonly CancellationTokenSource _cts = new();


    public FingerprintCaptureForm(ZktecoFingerprintService fingerService)
    {
        _fingerService = fingerService;
        InitializeComponent();
        FormClosing += OnFormClosing;
        Shown += OnShown;
    }

    private void InitializeComponent()
    {
        Text = "Capturar huella";
        ClientSize = new Size(420, 180);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Icon = IconLoader.Load("fingerprint");

        _lblStatus = new Label
        {
            Text = "Preparando lector...",
            AutoSize = false,
            Dock = DockStyle.Top,
            Height = 60,
            TextAlign = ContentAlignment.MiddleCenter
        };

        _progress = new ProgressBar
        {
            Dock = DockStyle.Top,
            Height = 28,
            Minimum = 0,
            Maximum = 3,
            Value = 0,
            Style = ProgressBarStyle.Continuous
        };

        _btnClose = new Button
        {
            Text = "Cerrar",
            Dock = DockStyle.Bottom,
            Height = 40,
            Enabled = false
        };
        _btnClose.Click += (_, _) => DialogResult = DialogResult.OK;

        Controls.Add(_btnClose);
        Controls.Add(_progress);
        Controls.Add(_lblStatus);
    }

    private async void OnShown(object? sender, EventArgs e)
    {
        Cursor = Cursors.WaitCursor;
        try
        {
            var result = await Task.Run(() =>
            {
                var ok = _fingerService.TryEnrollTemplateWithProgress(
                    out var tpl, out var msg,
                    (step, total, text) =>
                    {
                        BeginInvoke(new Action(() =>
                        {
                            _progress.Value = step <= _progress.Maximum ? step : _progress.Maximum;
                            _lblStatus.Text = text;
                        }));
                    },
                    _cts.Token);
                return (ok, tpl, msg);
            });


            Template = result.tpl;
            _lblStatus.Text = result.msg;
            _lblStatus.ForeColor = result.ok ? Color.ForestGreen : Color.Firebrick;
            DialogResult = result.ok ? DialogResult.OK : DialogResult.Cancel;
        }
        catch (Exception ex)
        {
            _lblStatus.Text = $"Error: {ex.Message}";
            _lblStatus.ForeColor = Color.Firebrick;
            DialogResult = DialogResult.Cancel;
        }
        finally
        {
            FormClosing -= OnFormClosing; // prevent Cancel after dispose
            Cursor = Cursors.Default;
            _btnClose.Enabled = true;
            _cts.Dispose();
        }
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        if (!_cts.IsCancellationRequested)
            _cts.Cancel();
    }
}
