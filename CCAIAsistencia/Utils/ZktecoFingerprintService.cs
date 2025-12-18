using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CCAIAsistencia.Models;
using libzkfpcsharp;

namespace CCAIAsistencia.Utils;

/// <summary>
/// Servicio de huellas para lectores ZKTeco usando libzkfpcsharp (SDK ZKFinger).
/// IMPORTANTE: NO usar DllImport("libzkfpcsharp.dll").
/// Se referencia como ensamblado .NET y se llama zkfp2.Init(), zkfp2.AcquireFingerprint(), etc.
/// </summary>
public sealed class ZktecoFingerprintService : IDisposable
{
    // Parámetros del SDK (según demos oficiales)
    private const int PARAM_IMAGE_WIDTH = 1;
    private const int PARAM_IMAGE_HEIGHT = 2;

    private const int MAX_TEMPLATE_SIZE = 2048;
    private const int REGISTER_FINGER_COUNT = 3;

    private readonly object _sync = new();

    private bool _initialized;
    private IntPtr _deviceHandle = IntPtr.Zero;
    private IntPtr _dbHandle = IntPtr.Zero;

    private int _imageWidth = 0;
    private int _imageHeight = 0;

    public bool IsReady => _initialized;

    public bool EnsureInitialized(out string message)
    {
        lock (_sync)
        {
            if (_initialized)
            {
                message = "SDK listo.";
                return true;
            }

            int ret = zkfp2.Init();
            if (ret != zkfperrdef.ZKFP_ERR_OK && ret != zkfperrdef.ZKFP_ERR_ALREADY_INIT)
            {
                message = $"Error al inicializar el SDK (código {ret}).";
                return false;
            }

            int count = zkfp2.GetDeviceCount();
            if (count <= 0)
            {
                message = "No se detecta ningún lector conectado.";
                return false;
            }

            _deviceHandle = zkfp2.OpenDevice(0);
            if (_deviceHandle == IntPtr.Zero)
            {
                message = "No se pudo abrir el lector (OpenDevice devolvió NULL).";
                return false;
            }

            _dbHandle = zkfp2.DBInit();
            if (_dbHandle == IntPtr.Zero)
            {
                message = "No se pudo inicializar la base interna de huellas (DBInit devolvió NULL).";
                return false;
            }

            _imageWidth = ReadIntParam(_deviceHandle, PARAM_IMAGE_WIDTH, fallback: 256);
            _imageHeight = ReadIntParam(_deviceHandle, PARAM_IMAGE_HEIGHT, fallback: 288);

            _initialized = true;
            message = $"SDK inicializado. Imagen: {_imageWidth}x{_imageHeight}.";
            return true;
        }
    }

    /// <summary>
    /// Captura una huella (1 intento). Si no hay dedo o falla lectura, devuelve false.
    /// </summary>
    public bool TryCaptureOnce(out byte[]? template, out string message)
    {
        template = null;

        lock (_sync)
        {
            if (!EnsureInitialized(out message))
                return false;

            byte[] image = new byte[_imageWidth * _imageHeight];
            byte[] capTmp = new byte[MAX_TEMPLATE_SIZE];
            int cbCapTmp = capTmp.Length;

            int ret = zkfp2.AcquireFingerprint(_deviceHandle, image, capTmp, ref cbCapTmp);
            if (ret != zkfperrdef.ZKFP_ERR_OK)
            {
                message = $"Lectura no exitosa (código {ret}). Coloca el dedo y vuelve a intentar.";
                return false;
            }

            template = capTmp.Take(cbCapTmp).ToArray();
            message = "Huella leída.";
            return true;
        }
    }

    public bool TryCaptureBlocking(out byte[]? template, out string message, int timeoutMs = 15000, CancellationToken cancellationToken = default)
    {
        template = null;
        message = "";

        lock (_sync)
        {
            if (!EnsureInitialized(out message))
                return false;

            var start = Environment.TickCount;

            while (Environment.TickCount - start < timeoutMs)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    message = "Captura cancelada.";
                    return false;
                }

                byte[] img = new byte[_imageWidth * _imageHeight];
                byte[] capTmp = new byte[MAX_TEMPLATE_SIZE];
                int cbCapTmp = capTmp.Length;

                int ret = zkfp2.AcquireFingerprint(_deviceHandle, img, capTmp, ref cbCapTmp);
                if (ret == zkfperrdef.ZKFP_ERR_OK && cbCapTmp > 0)
                {
                    template = capTmp.Take(cbCapTmp).ToArray();
                    message = "Huella capturada.";
                    return true;
                }

                // Tip: si quieres depurar, muestra ret:
                // message = $"Esperando dedo... ret={ret}";

                Thread.Sleep(120);
            }

            message = "No se detectó huella (timeout).";
            return false;
        }
    }

    public bool TryIdentifyStudentByMatch(
    IEnumerable<Alumno> students,
    out Alumno? match,
    out string message,
    out bool timedOut,
    int timeoutMs = 15000,
    int minScore = 60,
    CancellationToken cancellationToken = default)
    {
        match = null;
        message = "";
        timedOut = false;

        lock (_sync)
        {
            if (!EnsureInitialized(out message))
                return false;

            // 1) Captura "con espera"
            byte[] bestTpl;
            int bestSize;

            {
                var start = Environment.TickCount;
                int lastRet = 0;

                while (Environment.TickCount - start < timeoutMs)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        message = "Captura cancelada.";
                        return false;
                    }

                    byte[] img = new byte[_imageWidth * _imageHeight];
                    byte[] capTmp = new byte[2048];
                    int cbCapTmp = capTmp.Length;

                    lastRet = zkfp2.AcquireFingerprint(_deviceHandle, img, capTmp, ref cbCapTmp);
                    if (lastRet == zkfperrdef.ZKFP_ERR_OK && cbCapTmp > 0)
                    {
                        bestTpl = capTmp.Take(cbCapTmp).ToArray();
                        bestSize = cbCapTmp;
                        goto GOT_FINGER;
                    }

                    Thread.Sleep(120);
                }

                message = string.Empty;
                timedOut = true;
                return false;
            }

        GOT_FINGER:

            // 2) Compara contra los que sí tengan huella
            var candidates = students
                .Where(s => s.IsActive && s.Fingerprint is { Length: > 0 })
                .ToList();

            if (candidates.Count == 0)
            {
                message = "No hay coincidencia con huella registrada para comparar.";
                return false;
            }

            int bestScore = -1;
            Alumno? bestMatch = null;

            foreach (var s in candidates)
            {
                // score > 0 => coincide en algún nivel
                int score = zkfp2.DBMatch(_dbHandle, bestTpl, s.Fingerprint!);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestMatch = s;
                }
            }

            if (bestMatch != null && bestScore >= minScore)
            {
                match = bestMatch;
                message = $"Coincidencia OK (score={bestScore}).";
                return true;
            }

            message = $"Sin coincidencias en huella";
            return false;
        }
    }


    /// <summary>
    /// Enrola una huella: pide 3 lecturas del MISMO dedo y devuelve la plantilla final (merge).
    /// </summary>
    public bool TryEnrollTemplate(out byte[]? template, out string message, CancellationToken cancellationToken = default)
    {
        template = null;

        lock (_sync)
        {
            if (!EnsureInitialized(out message))
                return false;

            // buffers para 3 lecturas
            var regTmps = new byte[REGISTER_FINGER_COUNT][];
            for (int i = 0; i < REGISTER_FINGER_COUNT; i++)
                regTmps[i] = new byte[MAX_TEMPLATE_SIZE];

            int[] regSizes = new int[REGISTER_FINGER_COUNT];

            for (int i = 0; i < REGISTER_FINGER_COUNT; i++)
            {
                if (!CaptureTemplateBlocking(out var cap, out var capSize, out message, timeoutMs: 15000, cancellationToken))
                    return false;

                if (i > 0)
                {
                    // valida “mismo dedo”
                    int score = zkfp2.DBMatch(_dbHandle, cap, regTmps[i - 1]);
                    if (score <= 0)
                    {
                        message = "No coincide con la captura anterior. Usa el MISMO dedo para enrolar.";
                        return false;
                    }
                }

                Array.Copy(cap, 0, regTmps[i], 0, capSize);
                regSizes[i] = capSize;

                message = $"Lectura {i + 1}/{REGISTER_FINGER_COUNT} OK. Retira y vuelve a colocar el mismo dedo.";
            }

            // merge
            byte[] regTmp = new byte[MAX_TEMPLATE_SIZE];
            int cbRegTmp = regTmp.Length;

            int mergeRet = zkfp2.DBMerge(_dbHandle, regTmps[0], regTmps[1], regTmps[2], regTmp, ref cbRegTmp);
            if (mergeRet != zkfperrdef.ZKFP_ERR_OK || cbRegTmp <= 0)
            {
                // fallback: última lectura
                template = regTmps[REGISTER_FINGER_COUNT - 1].Take(regSizes[REGISTER_FINGER_COUNT - 1]).ToArray();
                message = $"No se pudo fusionar (código {mergeRet}). Se guardó la última lectura.";
                return true;
            }

            template = regTmp.Take(cbRegTmp).ToArray();
            message = "Huella registrada (plantilla final generada).";
            return true;
        }
    }

    /// <summary>
    /// Enrola mostrando progreso por lectura (callback: step, total, mensaje).
    /// </summary>
    public bool TryEnrollTemplateWithProgress(out byte[]? template, out string message, Action<int, int, string>? progress = null, CancellationToken cancellationToken = default)
    {
        template = null;
        lock (_sync)
        {
            if (!EnsureInitialized(out message))
                return false;

            var regTmps = new byte[REGISTER_FINGER_COUNT][];
            for (int i = 0; i < REGISTER_FINGER_COUNT; i++)
                regTmps[i] = new byte[MAX_TEMPLATE_SIZE];

            int[] regSizes = new int[REGISTER_FINGER_COUNT];

            for (int i = 0; i < REGISTER_FINGER_COUNT; i++)
            {
                progress?.Invoke(i + 1, REGISTER_FINGER_COUNT, $"Lectura {i + 1}/{REGISTER_FINGER_COUNT}: coloca el dedo.");
                if (!CaptureTemplateBlocking(out var cap, out var capSize, out message, timeoutMs: 15000, cancellationToken))
                {
                    progress?.Invoke(i + 1, REGISTER_FINGER_COUNT, message);
                    return false;
                }

                if (i > 0)
                {
                    int score = zkfp2.DBMatch(_dbHandle, cap, regTmps[i - 1]);
                    if (score <= 0)
                    {
                        message = "No coincide con la captura anterior. Usa el MISMO dedo para enrolar.";
                        progress?.Invoke(i + 1, REGISTER_FINGER_COUNT, message);
                        return false;
                    }
                }

                Array.Copy(cap, 0, regTmps[i], 0, capSize);
                regSizes[i] = capSize;
                progress?.Invoke(i + 1, REGISTER_FINGER_COUNT, $"Lectura {i + 1}/{REGISTER_FINGER_COUNT} OK. Retira y vuelve a colocar el mismo dedo.");
            }

            byte[] regTmp = new byte[MAX_TEMPLATE_SIZE];
            int cbRegTmp = regTmp.Length;

            int mergeRet = zkfp2.DBMerge(_dbHandle, regTmps[0], regTmps[1], regTmps[2], regTmp, ref cbRegTmp);
            if (mergeRet != zkfperrdef.ZKFP_ERR_OK || cbRegTmp <= 0)
            {
                template = regTmps[REGISTER_FINGER_COUNT - 1].Take(regSizes[REGISTER_FINGER_COUNT - 1]).ToArray();
                message = $"No se pudo fusionar (codigo {mergeRet}). Se guardo la ultima lectura.";
                progress?.Invoke(REGISTER_FINGER_COUNT, REGISTER_FINGER_COUNT, message);
                return true;
            }

            template = regTmp.Take(cbRegTmp).ToArray();
            message = "Huella registrada (plantilla final generada).";
            progress?.Invoke(REGISTER_FINGER_COUNT, REGISTER_FINGER_COUNT, message);
            return true;
        }
    }

    /// <summary>
    /// Captura huella en vivo y busca coincidencia contra alumnos (carga DB temporal con Matricula como fid).
    /// </summary>
    public bool TryIdentifyStudent(IEnumerable<Alumno> students, out Alumno? match, out string message, CancellationToken cancellationToken = default)
    {
        match = null;

        // Captura bloqueante para UX (15s)
        if (!CaptureTemplateBlocking(out var liveTpl, out var liveSize, out message, timeoutMs: 15000, cancellationToken))
            return false;

        var candidates = students
            .Where(s => s.IsActive && s.Fingerprint is { Length: > 0 })
            .Select(s => (Id: s.Matricula, Template: s.Fingerprint!));

        if (!TryIdentify(liveTpl.Take(liveSize).ToArray(), candidates, out int matchedId, out message))
            return false;

        match = students.FirstOrDefault(s => s.Matricula == matchedId);
        if (match is null)
        {
            message = "Coincidió el ID, pero no se encontró el alumno en la lista.";
            return false;
        }

        return true;
    }

    /// <summary>
    /// Identifica una plantilla viva contra un conjunto de plantillas (Id -> template).
    /// </summary>
    public bool TryIdentify(
        byte[] liveTemplate,
        IEnumerable<(int Id, byte[] Template)> templates,
        out int matchedId,
        out string message)
    {
        matchedId = -1;
        message = string.Empty;

        var list = templates.Where(t => t.Template is { Length: > 0 }).ToList();
        if (list.Count == 0)
        {
            message = "No hay plantillas para comparar.";
            return false;
        }

        lock (_sync)
        {
            if (!EnsureInitialized(out message))
                return false;

            zkfp2.DBClear(_dbHandle);

            foreach (var item in list)
            {
                int addRet = zkfp2.DBAdd(_dbHandle, item.Id, item.Template);
                if (addRet != zkfperrdef.ZKFP_ERR_OK)
                {
                    message = $"No se pudo cargar plantilla en DB (ID {item.Id}, código {addRet}).";
                    return false;
                }
            }

            int fid = -1;
            int score = 0;

            int identifyRet = zkfp2.DBIdentify(_dbHandle, liveTemplate, ref fid, ref score);
            if (identifyRet == zkfperrdef.ZKFP_ERR_OK && fid >= 0)
            {
                matchedId = fid;
                message = $"Coincidencia encontrada (score {score}).";
                return true;
            }

            message = $"No se encontró coincidencia (código {identifyRet}).";
            return false;
        }
    }

    public void Dispose()
    {
        lock (_sync)
        {
            if (_dbHandle != IntPtr.Zero)
            {
                zkfp2.DBClear(_dbHandle);
                _dbHandle = IntPtr.Zero;
            }

            if (_deviceHandle != IntPtr.Zero)
            {
                zkfp2.CloseDevice(_deviceHandle);
                _deviceHandle = IntPtr.Zero;
            }

            if (_initialized)
            {
                zkfp2.Terminate();
                _initialized = false;
            }
        }
    }

    // ------------------------
    // Helpers
    // ------------------------

    private static int ReadIntParam(IntPtr devHandle, int paramId, int fallback)
    {
        byte[] buf = new byte[4];
        int size = 4;
        int ret = zkfp2.GetParameters(devHandle, paramId, buf, ref size);
        if (ret != zkfperrdef.ZKFP_ERR_OK) return fallback;

        int value = 0;
        zkfp2.ByteArray2Int(buf, ref value);
        return value > 0 ? value : fallback;
    }

    /// <summary>
    /// Captura hasta que haya huella o se agote el timeout.
    /// </summary>
    private bool CaptureTemplateBlocking(out byte[] templateBuf, out int templateSize, out string message, int timeoutMs, CancellationToken cancellationToken)
    {
        templateBuf = new byte[MAX_TEMPLATE_SIZE];
        templateSize = 0;
        message = string.Empty;

        var start = Environment.TickCount;

        while (Environment.TickCount - start < timeoutMs)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                message = "Captura cancelada.";
                return false;
            }

            byte[] image = new byte[_imageWidth * _imageHeight];
            int cbCapTmp = templateBuf.Length;

            int ret = zkfp2.AcquireFingerprint(_deviceHandle, image, templateBuf, ref cbCapTmp);
            if (ret == zkfperrdef.ZKFP_ERR_OK && cbCapTmp > 0)
            {
                templateSize = cbCapTmp;
                message = "Huella capturada.";
                return true;
            }

            // No saturar CPU
            Thread.Sleep(120);
        }

        message = "Tiempo de espera agotado. No se capturó huella.";
        return false;
    }
}

