using System.Globalization;
using System.Text;
#if WINDOWS
using DrawingGraphics = System.Drawing.Graphics;
using DrawingFont = System.Drawing.Font;
using DrawingSolidBrush = System.Drawing.SolidBrush;
using DrawingPen = System.Drawing.Pen;
using DrawingFontStyle = System.Drawing.FontStyle;
using DrawingRectangleF = System.Drawing.RectangleF;
using DrawingStringFormat = System.Drawing.StringFormat;
using DrawingStringTrimming = System.Drawing.StringTrimming;
using DrawingStringAlignment = System.Drawing.StringAlignment;
using DrawingColor = System.Drawing.Color;
using DrawingPrintDocument = System.Drawing.Printing.PrintDocument;
using DrawingStandardPrintController = System.Drawing.Printing.StandardPrintController;
#endif

namespace ApotekApp.Services;

/// <summary>Printer laporan profesional untuk Windows. Mendukung header, tabel multi-halaman, footer, dan nomor halaman.</summary>
public static class ReportPrintService
{
    public static bool Print(string title, string subtitle, IEnumerable<string[]> rows)
        => PrintTable(title, subtitle, Array.Empty<string>(), rows, string.Empty);

    public static bool PrintTable(string title, string subtitle, string[] headers, IEnumerable<string[]> rows, string footer)
    {
#if WINDOWS
        try
        {
            var printer = RawPrinterHelper.GetDefaultPrinterName();
            if (string.IsNullOrWhiteSpace(printer)) return false;

            var data = rows.Select(r => r?.Select(Clean).ToArray() ?? Array.Empty<string>()).ToList();
            var cols = Math.Max(headers?.Length ?? 0, data.Select(x => x.Length).DefaultIfEmpty(0).Max());
            using var doc = new DrawingPrintDocument();
            doc.PrinterSettings.PrinterName = printer;
            if (!doc.PrinterSettings.IsValid) return false;
            doc.DocumentName = title?.Trim() ?? "Laporan Apotek";
            doc.DefaultPageSettings.Landscape = true;
            doc.PrintController = new DrawingStandardPrintController();

            var index = 0;
            var page = 0;
            var widths = BuildColumnWidths(headers ?? Array.Empty<string>(), cols);
            doc.PrintPage += (_, e) =>
            {
                page++;
                var g = e.Graphics;
                var bounds = e.MarginBounds;
                using var titleFont = new DrawingFont("Segoe UI", 15, DrawingFontStyle.Bold);
                using var subtitleFont = new DrawingFont("Segoe UI", 9, DrawingFontStyle.Regular);
                using var headFont = new DrawingFont("Segoe UI", 8, DrawingFontStyle.Bold);
                using var bodyFont = new DrawingFont("Segoe UI", 8, DrawingFontStyle.Regular);
                using var footerFont = new DrawingFont("Segoe UI", 8, DrawingFontStyle.Bold);
                using var linePen = new DrawingPen(DrawingColor.FromArgb(210, 220, 232), 1);
                using var headerBrush = new DrawingSolidBrush(DrawingColor.FromArgb(242, 246, 250));
                using var textBrush = new DrawingSolidBrush(DrawingColor.FromArgb(28, 43, 65));
                using var mutedBrush = new DrawingSolidBrush(DrawingColor.FromArgb(92, 108, 128));

                var y = bounds.Top;
                g.DrawString(Clean(title), titleFont, textBrush, bounds.Left, y); y += 28;
                if (!string.IsNullOrWhiteSpace(subtitle)) { g.DrawString(Clean(subtitle), subtitleFont, mutedBrush, bounds.Left, y); y += 18; }
                g.DrawLine(linePen, bounds.Left, y, bounds.Right, y); y += 8;

                if (cols > 0 && headers.Length > 0)
                {
                    var h = 26;
                    g.FillRectangle(headerBrush, bounds.Left, y, bounds.Width, h);
                    DrawRow(g, headers, widths, bounds.Left, y, h, headFont, textBrush, linePen, true);
                    y += h;
                }

                var rowHeight = 25;
                while (index < data.Count)
                {
                    if (y + rowHeight > bounds.Bottom - 30) break;
                    DrawRow(g, data[index], widths, bounds.Left, y, rowHeight, bodyFont, textBrush, linePen, false);
                    y += rowHeight;
                    index++;
                }

                if (index >= data.Count && !string.IsNullOrWhiteSpace(footer))
                {
                    y += 8;
                    g.DrawLine(linePen, bounds.Left, y, bounds.Right, y); y += 8;
                    g.DrawString(Clean(footer), footerFont, textBrush, bounds.Left, y);
                }

                var pageText = $"Apotek • Dicetak {DateTime.Now:dd/MM/yyyy HH:mm} • Halaman {page}";
                var size = g.MeasureString(pageText, subtitleFont);
                g.DrawString(pageText, subtitleFont, mutedBrush, bounds.Right - size.Width, e.PageBounds.Bottom - 32);
                e.HasMorePages = index < data.Count;
            };

            doc.Print();
            return true;
        }
        catch { return false; }
#else
        return false;
#endif
    }

    private static string Clean(string? value) => string.IsNullOrWhiteSpace(value) ? "" : value.Replace('\r',' ').Replace('\n',' ').Trim();

#if WINDOWS
    private static int[] BuildColumnWidths(string[] headers, int count)
    {
        if (count <= 0) return Array.Empty<int>();
        var weights = Enumerable.Range(0, count).Select(i =>
        {
            var h = i < headers.Length ? Clean(headers[i]).ToUpperInvariant() : "";
            if (h.Contains("KETERANGAN") || h.Contains("SUPPLIER")) return 2.0;
            if (h.Contains("NAMA")) return 1.7;
            if (h.Contains("TANGGAL") || h.Contains("NOMOR")) return 1.1;
            return 0.75;
        }).ToArray();
        var total = weights.Sum();
        // Relative widths; DrawRow scales them to the actual page width.
        return weights.Select(w => (int)Math.Round(w / total * 1000)).ToArray();
    }

    private static void DrawRow(DrawingGraphics g, string[] row, int[] relativeWidths, float x, float y, float height, DrawingFont font, DrawingSolidBrush brush, DrawingPen pen, bool header)
    {
        if (relativeWidths.Length == 0) return;
        var total = relativeWidths.Sum();
        var left = x;
        var fullWidth = relativeWidths.Length > 0 ? g.VisibleClipBounds.Width - x - 30 : 0;
        for (var i = 0; i < relativeWidths.Length; i++)
        {
            var width = fullWidth * relativeWidths[i] / total;
            var value = i < row.Length ? Clean(row[i]) : "";
            var rect = new DrawingRectangleF(left + 5, y + 3, Math.Max(20, width - 10), height - 6);
            var flags = DrawingStringFormat.GenericTypographic;
            flags.Trimming = DrawingStringTrimming.EllipsisCharacter;
            flags.Alignment = IsNumeric(value) ? DrawingStringAlignment.Far : DrawingStringAlignment.Near;
            flags.LineAlignment = DrawingStringAlignment.Center;
            g.DrawString(value, font, brush, rect, flags);
            g.DrawLine(pen, left, y + height - 1, left + width, y + height - 1);
            left += width;
        }
    }

    private static bool IsNumeric(string value)
    {
        var s = value.Replace("Rp", "", StringComparison.OrdinalIgnoreCase).Replace(".", "").Replace(",", ".").Trim();
        return decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out _);
    }
#endif
}
