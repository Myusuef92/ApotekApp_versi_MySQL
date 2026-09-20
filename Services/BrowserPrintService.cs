using System.Diagnostics;
using System.Net;
using System.Text;
using Microsoft.Maui.Storage;

namespace ApotekApp.Services;

/// <summary>
/// Membuka dokumen cetak di browser default dan memanggil window.print().
/// Browser akan menampilkan Print Preview/dialog sehingga pengguna dapat
/// memilih printer. Aplikasi tidak langsung menyimpan sebagai PDF.
/// </summary>
public static class BrowserPrintService
{
    public static async Task<bool> OpenPrintPreviewAsync(string title, string bodyHtml, string css = "")
    {
        try
        {
            var fileName = $"ApotekPOS_Print_{DateTime.Now:yyyyMMdd_HHmmss_fff}.html";
            var path = Path.Combine(FileSystem.CacheDirectory, fileName);
            var html = BuildDocument(title, bodyHtml, css);
            await File.WriteAllTextAsync(path, html, new UTF8Encoding(false));

            // Jangan menggunakan asosiasi file Windows (UseShellExecute + path HTML),
            // karena pada sebagian PC .html terasosiasi ke Notepad/VS Code.
            // Buka file secara eksplisit melalui browser yang terpasang agar
            // window.print() selalu masuk ke Print Preview browser.
            var fileUri = new Uri(path).AbsoluteUri;
            var browser = FindBrowserExecutable();
            if (string.IsNullOrWhiteSpace(browser)) return false;

            var started = Process.Start(new ProcessStartInfo
            {
                FileName = browser,
                Arguments = QuoteArgument(fileUri),
                UseShellExecute = false,
                CreateNoWindow = true
            });

            if (started == null) return false;

            // Jangan hapus segera karena browser masih membaca file.
            _ = Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromMinutes(15));
                try { if (File.Exists(path)) File.Delete(path); } catch { }
            });

            return true;
        }
        catch
        {
            return false;
        }
    }

    public static string ReceiptHtml(
        string namaApotek, string alamat, string telepon, string sia,
        string noNota, DateTime tanggal,
        IEnumerable<(string NamaObat, int Qty, decimal Harga, decimal Subtotal)> items,
        decimal subtotal, decimal diskon, decimal ppn, decimal grandTotal,
        decimal bayar, decimal kembali, string footer, string ukuranKertas)
    {
        var width = ukuranKertas == "80mm" ? "80mm" : "58mm";
        var itemHtml = new StringBuilder();
        foreach (var item in items)
        {
            itemHtml.Append($"<div class='item'><div class='name'>{E(item.NamaObat)}</div><div>{item.Qty:N0} x Rp {item.Harga:N0}<span>Rp {item.Subtotal:N0}</span></div></div>");
        }

        return $@"
<div class='receipt' style='--paper-width:{width}'>
  <header>
    <div class='store'>{E(namaApotek)}</div>
    <div>{E(alamat)}</div>
    {(string.IsNullOrWhiteSpace(telepon) ? "" : $"<div>Telp: {E(telepon)}</div>")}
    {(string.IsNullOrWhiteSpace(sia) ? "" : $"<div>{E(sia)}</div>")}
  </header>
  <hr>
  <div class='meta'><div>Nota: {E(noNota)}</div><div>Tgl: {tanggal:dd/MM/yyyy HH:mm}</div></div>
  <hr>
  {itemHtml}
  <hr>
  <div class='totals'>
    <div>Subtotal<span>Rp {subtotal:N0}</span></div>
    <div>Diskon<span>Rp {diskon:N0}</span></div>
    <div>PPN<span>Rp {ppn:N0}</span></div>
    <div class='grand'>Grand Total<span>Rp {grandTotal:N0}</span></div>
    <div>Bayar<span>Rp {bayar:N0}</span></div>
    <div class='change'>Kembali<span>Rp {kembali:N0}</span></div>
  </div>
  <hr>
  <div class='footer'>{E(footer)}</div>
</div>";
    }

    public static string ReportHtml(string title, string subtitle, string[] headers, IEnumerable<string[]> rows, string footer)
    {
        var sb = new StringBuilder();
        sb.Append("<div class='report'><h1>").Append(E(title)).Append("</h1><div class='subtitle'>").Append(E(subtitle)).Append("</div><table><thead><tr>");
        foreach (var h in headers) sb.Append("<th>").Append(E(h)).Append("</th>");
        sb.Append("</tr></thead><tbody>");
        foreach (var row in rows)
        {
            sb.Append("<tr>");
            for (var i = 0; i < headers.Length; i++) sb.Append("<td>").Append(E(i < row.Length ? row[i] : "")).Append("</td>");
            sb.Append("</tr>");
        }
        sb.Append("</tbody></table><div class='report-footer'>").Append(E(footer)).Append("</div></div>");
        return sb.ToString();
    }

    public static string BarcodeHtml(string productName, string code, string barcode, int copies = 1)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < Math.Max(1, copies); i++)
        {
            sb.Append("<div class='label'>")
              .Append("<div class='product'>").Append(E(productName)).Append("</div>")
              .Append("<div class='code'>Kode: ").Append(E(code)).Append("</div>")
              .Append(CreateBarcodeSvg(barcode))
              .Append("<div class='barcode-text'>").Append(E(barcode)).Append("</div></div>");
        }
        return sb.ToString();
    }

    private static string BuildDocument(string title, string bodyHtml, string css)
    {
        return $@"<!doctype html><html><head><meta charset='utf-8'><title>{E(title)}</title>
<style>
*{{box-sizing:border-box}} html,body{{margin:0;padding:0;background:#fff;color:#111827;font-family:Arial,Helvetica,sans-serif}} body{{padding:0}}
{css}
@media print{{body{{padding:0}}}}
</style></head><body>{bodyHtml}
<script>window.addEventListener('load',function(){{setTimeout(function(){{window.print();}},350);}});</script>
</body></html>";
    }


    private static string? FindBrowserExecutable()
    {
#if WINDOWS
        var candidates = new List<string>();
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

        void Add(string baseDir, string relative)
        {
            if (!string.IsNullOrWhiteSpace(baseDir))
                candidates.Add(Path.Combine(baseDir, relative));
        }

        // Edge
        Add(programFiles, @"Microsoft\Edge\Application\msedge.exe");
        Add(programFilesX86, @"Microsoft\Edge\Application\msedge.exe");
        // Chrome
        Add(local, @"Google\Chrome\Application\chrome.exe");
        Add(programFiles, @"Google\Chrome\Application\chrome.exe");
        Add(programFilesX86, @"Google\Chrome\Application\chrome.exe");
        // Brave / Firefox sebagai fallback
        Add(local, @"BraveSoftware\Brave-Browser\Application\brave.exe");
        Add(programFiles, @"BraveSoftware\Brave-Browser\Application\brave.exe");
        Add(programFilesX86, @"BraveSoftware\Brave-Browser\Application\brave.exe");
        Add(programFiles, @"Mozilla Firefox\firefox.exe");
        Add(programFilesX86, @"Mozilla Firefox\firefox.exe");

        foreach (var candidate in candidates)
            if (File.Exists(candidate)) return candidate;

        // Beberapa instalasi browser menambahkan executable ke PATH.
        foreach (var command in new[] { "msedge.exe", "chrome.exe", "brave.exe", "firefox.exe" })
        {
            try
            {
                using var p = Process.Start(new ProcessStartInfo
                {
                    FileName = "where.exe",
                    Arguments = command,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                });
                if (p == null) continue;
                var result = p.StandardOutput.ReadLine();
                p.WaitForExit(1000);
                if (!string.IsNullOrWhiteSpace(result) && File.Exists(result.Trim()))
                    return result.Trim();
            }
            catch { }
        }
#endif
        return null;
    }

    private static string QuoteArgument(string value) => $"\"{value.Replace("\"", "\\\"")}\"";

    private static string CreateBarcodeSvg(string value)
    {
        var patterns = new[]
        {
            "212222","222122","222221","121223","121322","131222","122213","122312","132212","221213","221312","231212","112232","122132","122231","113222","123122","123221","223211","221132","221231","213212","223112","312131","311222","321122","321221","312212","322112","322211","212123","212321","232121","111323","131123","131321","112313","132113","132311","211313","231113","231311","112133","112331","132131","113123","113321","133121","313121","211331","231131","213113","223112","213131","311123","311321","331121","312113","312311","332111","314111","221411","431111","111224","111422","121124","121421","141122","141221","112214","112412","122114","122411","142112","142211","241211","221114","413111","241112","134111","111242","121142","121241","114212","124112","124211","411212","421112","421211","212141","214121","412121","111143","111341","131141","114113","114311","411113","411311","113141","114131","311141","411131","211412","211214","211232","2331112"
        };
        if (string.IsNullOrWhiteSpace(value)) return "";
        var codes = new List<int> { 104 };
        foreach (var ch in value) if (ch >= 32 && ch <= 126) codes.Add(ch - 32);
        var checksum = 104;
        for (var i = 1; i < codes.Count; i++) checksum += codes[i] * i;
        codes.Add(checksum % 103); codes.Add(106);
        var modules = codes.Sum(c => c == 106 ? 13 : 11);
        var module = 2.0; var quiet = 10.0;
        var width = (modules + quiet * 2) * module;
        var x = quiet * module;
        var bars = new StringBuilder();
        foreach (var c in codes)
        {
            var black = true;
            foreach (var ch in patterns[c])
            {
                var w = (ch - '0') * module;
                if (black) bars.Append($"<rect x='{x:0.##}' y='0' width='{w:0.##}' height='70'/>");
                x += w; black = !black;
            }
        }
        return $"<svg class='barcode' xmlns='http://www.w3.org/2000/svg' viewBox='0 0 {width:0.##} 70' preserveAspectRatio='none'>{bars}</svg>";
    }

    private static string E(string? value) => WebUtility.HtmlEncode(value ?? "");
}
