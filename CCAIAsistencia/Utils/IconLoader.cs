using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;

namespace CCAIAsistencia.Utils;

/// <summary>
/// Carga Icon a partir de PNG o ICO desde la carpeta Assets/Icons.
/// Usa PNG por defecto para evitar fondos negros en íconos.
/// </summary>
public static class IconLoader
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    public static Icon? Load(string name)
    {
        var baseDir = AppContext.BaseDirectory;
        var pngPath = Path.Combine(baseDir, "Assets", "Icons", $"{name}.png");
        var icoPath = Path.Combine(baseDir, "Assets", "Icons", $"{name}.ico");

        if (File.Exists(pngPath))
        {
            using var bmp = new Bitmap(pngPath);
            var hIcon = bmp.GetHicon();
            try
            {
                var icon = Icon.FromHandle(hIcon);
                return (Icon)icon.Clone(); // Clonar para poder liberar handle
            }
            finally
            {
                DestroyIcon(hIcon);
            }
        }

        if (File.Exists(icoPath))
        {
            return new Icon(icoPath);
        }

        return null;
    }
}
