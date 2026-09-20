using DrawingColor = System.Drawing.Color;
using DrawingFont = System.Drawing.Font;
using DrawingFontStyle = System.Drawing.FontStyle;
using DrawingRectangleF = System.Drawing.RectangleF;
using DrawingStringFormat = System.Drawing.StringFormat;
using DrawingStringAlignment = System.Drawing.StringAlignment;
using DrawingSolidBrush = System.Drawing.SolidBrush;
using DrawingPrintDocument = System.Drawing.Printing.PrintDocument;
using DrawingPrinterSettings = System.Drawing.Printing.PrinterSettings;
using DrawingStandardPrintController = System.Drawing.Printing.StandardPrintController;
using System.Globalization;

namespace ApotekApp.Services;

public static class BarcodePrintService
{
#if WINDOWS
    private static readonly string[] Patterns =
    {
        "212222","222122","222221","121223","121322","131222","122213","122312","132212","221213","221312","231212","112232","122132","122231","113222","123122","123221","223211","221132","221231","213212","223112","312131","311222","321122","321221","312212","322112","322211","212123","212321","232121","111323","131123","131321","112313","132113","132311","211313","231113","231311","112133","112331","132131","113123","113321","133121","313121","211331","231131","213113","213311","213131","311123","311321","331121","312113","312311","332111","314111","221411","431111","111224","111422","121124","121421","141122","141221","112214","112412","122114","122411","142112","142211","241211","221114","413111","241112","134111","111242","121142","121241","114212","124112","124211","411212","421112","421211","212141","214121","412121","111143","111341","131141","114113","114311","411113","411311","113141","114131","311141","411131","211412","211214","211232","2331112"
    };

    public static bool Print(string barcode, string productName, string code, int copies = 1)
    {
        if (string.IsNullOrWhiteSpace(barcode)) return false;
        var printer = DrawingPrinterSettings.InstalledPrinters.Count > 0 ? new DrawingPrinterSettings().PrinterName : string.Empty;
        if (string.IsNullOrWhiteSpace(printer)) return false;

        using var doc = new DrawingPrintDocument();
        doc.PrinterSettings.PrinterName = printer;
        if (!doc.PrinterSettings.IsValid) return false;
        doc.DocumentName = $"Barcode {productName}";
        doc.PrintController = new DrawingStandardPrintController();

        var remaining = Math.Max(1, copies);
        doc.PrintPage += (_, e) =>
        {
            var bounds = e.PageBounds;
            var margin = 24;
            var centerX = bounds.Left + bounds.Width / 2f;
            using var titleFont = new DrawingFont("Arial", 10, DrawingFontStyle.Bold);
            using var smallFont = new DrawingFont("Arial", 8, DrawingFontStyle.Regular);
            using var codeFont = new DrawingFont("Arial", 9, DrawingFontStyle.Bold);
            using var brush = new DrawingSolidBrush(DrawingColor.Black);

            var name = productName ?? "";
            e.Graphics.DrawString(name, titleFont, brush,
                new DrawingRectangleF(margin, 10, bounds.Width - margin * 2, 24),
                new DrawingStringFormat { Alignment = DrawingStringAlignment.Center });
            e.Graphics.DrawString($"Kode: {code}", smallFont, brush,
                new DrawingRectangleF(margin, 32, bounds.Width - margin * 2, 20),
                new DrawingStringFormat { Alignment = DrawingStringAlignment.Center });

            var codes = new List<int> { 104 };
            foreach (var ch in barcode)
                if (ch >= 32 && ch <= 126) codes.Add(ch - 32);
            var checksum = 104;
            for (var i = 1; i < codes.Count; i++) checksum += codes[i] * i;
            codes.Add(checksum % 103);
            codes.Add(106);

            var modules = codes.Sum(c => c == 106 ? 13 : 11);
            var available = Math.Max(120, bounds.Width - margin * 2);
            var module = Math.Max(1.0, Math.Min(3.0, available / (modules + 20.0)));
            var totalWidth = (float)((modules + 20) * module);
            var x = centerX - totalWidth / 2f + (float)(10 * module);
            const float y = 60;
            const float height = 72;

            foreach (var c in codes)
            {
                var pattern = Patterns[c];
                var black = true;
                foreach (var digit in pattern)
                {
                    var w = (float)((digit - '0') * module);
                    if (black) e.Graphics.FillRectangle(brush, x, y, w, height);
                    x += w;
                    black = !black;
                }
            }

            e.Graphics.DrawString(barcode, codeFont, brush,
                new DrawingRectangleF(margin, y + height + 5, bounds.Width - margin * 2, 24),
                new DrawingStringFormat { Alignment = DrawingStringAlignment.Center });

            remaining--;
            e.HasMorePages = remaining > 0;
        };

        try
        {
            doc.Print();
            return true;
        }
        catch
        {
            return false;
        }
    }
#endif
}
