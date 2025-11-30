using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using OfficeOpenXml;

namespace CCAIAsistencia.Utils;

/// <summary>
/// Utilidad para exportar listas a Excel usando EPPlus.
/// </summary>
public static class Export
{
    public static void GuardarArchivoExcel<T>(IList<T> lista, string nombreHoja = "Datos")
    {
        if (lista == null || lista.Count == 0)
        {
            MessageBox.Show("La lista está vacía o es nula.", "Exportar", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dialog = new SaveFileDialog
        {
            Filter = "Excel files (*.xlsx)|*.xlsx",
            Title = "Guardar archivo Excel",
            FileName = "datos.xlsx"
        };

        if (dialog.ShowDialog() != DialogResult.OK)
        {
            return;
        }

        ExportarAExcel(lista, dialog.FileName, nombreHoja);
    }

    public static void ExportarAExcel<T>(IList<T> lista, string rutaArchivo, string nombreHoja = "Datos")
    {
        if (lista == null || lista.Count == 0)
        {
            MessageBox.Show("La lista está vacía o es nula.", "Exportar", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        using var excelPackage = new ExcelPackage();
        var worksheet = excelPackage.Workbook.Worksheets.Add(nombreHoja);
        var properties = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public);

        for (var i = 0; i < properties.Length; i++)
        {
            worksheet.Cells[1, i + 1].Value = properties[i].Name;
        }

        var row = 2;
        foreach (var item in lista)
        {
            for (var col = 0; col < properties.Length; col++)
            {
                var value = properties[col].GetValue(item);
                worksheet.Cells[row, col + 1].Value = value ?? string.Empty;
            }
            row++;
        }

        worksheet.Cells[worksheet.Dimension!.Address].AutoFitColumns();
        excelPackage.SaveAs(new FileInfo(rutaArchivo));

        MessageBox.Show($"Archivo Excel exportado correctamente en:\n{rutaArchivo}", "Exportar", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}
