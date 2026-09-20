using DrawingPrintDocument = System.Drawing.Printing.PrintDocument;
using DrawingStandardPrintController = System.Drawing.Printing.StandardPrintController;
using DrawingPaperSize = System.Drawing.Printing.PaperSize;
using DrawingMargins = System.Drawing.Printing.Margins;
using DrawingFont = System.Drawing.Font;
using DrawingFontStyle = System.Drawing.FontStyle;
using DrawingSolidBrush = System.Drawing.SolidBrush;
using DrawingPen = System.Drawing.Pen;
using DrawingColor = System.Drawing.Color;
using DrawingRectangleF = System.Drawing.RectangleF;
using DrawingStringFormat = System.Drawing.StringFormat;
using DrawingStringAlignment = System.Drawing.StringAlignment;
using DrawingStringTrimming = System.Drawing.StringTrimming;

namespace ApotekApp.Services;

/// <summary>
/// Cetak struk langsung ke printer Windows default tanpa membuka browser
/// atau dialog print. Cocok untuk printer thermal 58/80 mm maupun printer
/// Windows biasa yang sudah dikonfigurasi sebagai default printer.
/// </summary>
public static class ReceiptPrintService
{
#if WINDOWS
    public static bool Print(
        string printerName,
        string namaApotek,
        string alamat,
        string telepon,
        string sia,
        string nomorNota,
        DateTime tanggal,
        IEnumerable<(string Nama, int Qty, decimal Harga, decimal Subtotal)> items,
        decimal subtotal,
        decimal diskon,
        decimal ppn,
        decimal grandTotal,
        decimal bayar,
        decimal kembali,
        string footer,
        string ukuranKertas = "58mm")
    {
        try
        {
            if (string.IsNullOrWhiteSpace(printerName))
                printerName = RawPrinterHelper.GetDefaultPrinterName();

            if (string.IsNullOrWhiteSpace(printerName)) return false;

            using var doc = new DrawingPrintDocument();
            doc.PrinterSettings.PrinterName = printerName;
            if (!doc.PrinterSettings.IsValid) return false;

            doc.DocumentName = $"Struk {nomorNota}";
            doc.PrintController = new DrawingStandardPrintController();
            var data = items.ToList();
            var printed = false;

            // Ukuran ini hanya fallback. Driver thermal tetap menjadi sumber
            // konfigurasi utama untuk ukuran kertas fisik.
            try
            {
                var width = ukuranKertas.Equals("80mm", StringComparison.OrdinalIgnoreCase) ? 315 : 228;
                doc.DefaultPageSettings.PaperSize = new DrawingPaperSize("ApotekPOS", width, Math.Min(3000, 900 + data.Count * 42));
                doc.DefaultPageSettings.Margins = new DrawingMargins(8, 8, 8, 8);
            }
            catch
            {
                // Sebagian driver printer tidak mengizinkan PaperSize custom.
            }

            doc.PrintPage += (_, e) =>
            {
                printed = true;
                var g = e.Graphics;
                var bounds = e.MarginBounds;
                var center = new DrawingStringFormat { Alignment = DrawingStringAlignment.Center };
                var right = new DrawingStringFormat { Alignment = DrawingStringAlignment.Far };
                var left = new DrawingStringFormat { Alignment = DrawingStringAlignment.Near, Trimming = DrawingStringTrimming.EllipsisCharacter };

                using var titleFont = new DrawingFont("Arial", ukuranKertas == "80mm" ? 12 : 10, DrawingFontStyle.Bold);
                using var normalFont = new DrawingFont("Courier New", ukuranKertas == "80mm" ? 8.5f : 7.5f, DrawingFontStyle.Regular);
                using var boldFont = new DrawingFont("Courier New", ukuranKertas == "80mm" ? 8.5f : 7.5f, DrawingFontStyle.Bold);
                using var totalFont = new DrawingFont("Courier New", ukuranKertas == "80mm" ? 9.5f : 8.5f, DrawingFontStyle.Bold);
                using var brush = new DrawingSolidBrush(DrawingColor.Black);
                using var linePen = new DrawingPen(DrawingColor.Black, 1);

                float y = bounds.Top;
                float w = bounds.Width;
                float rowGap = 14;

                void DrawCenter(string text, DrawingFont font, float height = 16)
                {
                    g.DrawString(Clean(text), font, brush, new DrawingRectangleF(bounds.Left, y, w, height), center);
                    y += height;
                }

                void DrawPair(string label, string value, DrawingFont font = null!)
                {
                    font ??= normalFont;
                    g.DrawString(Clean(label), font, brush, new DrawingRectangleF(bounds.Left, y, w * .55f, rowGap), left);
                    g.DrawString(Clean(value), font, brush, new DrawingRectangleF(bounds.Left, y, w, rowGap), right);
                    y += rowGap;
                }

                void DrawLine()
                {
                    y += 2;
                    g.DrawLine(linePen, bounds.Left, y, bounds.Right, y);
                    y += 6;
                }

                DrawCenter(namaApotek, titleFont, 22);
                if (!string.IsNullOrWhiteSpace(alamat)) DrawCenter(alamat, normalFont);
                if (!string.IsNullOrWhiteSpace(telepon)) DrawCenter($"Telp: {telepon}", normalFont);
                if (!string.IsNullOrWhiteSpace(sia)) DrawCenter(sia, normalFont);
                DrawLine();
                DrawPair("Nota", nomorNota, boldFont);
                DrawPair("Tanggal", tanggal.ToString("dd/MM/yyyy HH:mm"));
                DrawLine();

                foreach (var item in data)
                {
                    var itemName = Clean(item.Nama);
                    var itemHeight = Math.Max(16, g.MeasureString(itemName, boldFont, (int)w).Height);
                    g.DrawString(itemName, boldFont, brush, new DrawingRectangleF(bounds.Left, y, w, itemHeight), left);
                    y += itemHeight;
                    DrawPair($"{item.Qty:N0} x {item.Harga:N0}", $"Rp {item.Subtotal:N0}");
                }

                DrawLine();
                DrawPair("Subtotal", $"Rp {subtotal:N0}");
                DrawPair("Diskon", $"Rp {diskon:N0}");
                DrawPair($"PPN", $"Rp {ppn:N0}");
                DrawPair("TOTAL", $"Rp {grandTotal:N0}", totalFont);
                DrawPair("Bayar", $"Rp {bayar:N0}");
                DrawPair("Kembali", $"Rp {kembali:N0}", totalFont);
                DrawLine();
                DrawCenter(footer, normalFont, 28);
                DrawCenter("Dicetak oleh ApotekPOS", normalFont, 16);
                e.HasMorePages = false;
            };

            doc.Print();
            return printed;
        }
        catch
        {
            return false;
        }
    }
#endif

    private static string Clean(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? ""
            : value.Replace('\r', ' ').Replace('\n', ' ').Trim();
}
