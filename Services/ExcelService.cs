using System.IO.Compression;
using System.Text;
using System.Xml.Linq;

namespace ApotekApp.Services;

public static class ExcelService
{
    public static async Task SaveAsync(string fileName, string[] headers, IEnumerable<string[]> rows)
    {
#if WINDOWS
        var picker = new Windows.Storage.Pickers.FileSavePicker
        {
            SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Downloads,
            SuggestedFileName = fileName
        };
        picker.FileTypeChoices.Add("Excel Workbook", new List<string> { ".xlsx" });
        var window = Application.Current?.Windows.FirstOrDefault()?.Handler?.PlatformView as Microsoft.UI.Xaml.Window;
        if (window is not null)
        {
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
        }
        var file = await picker.PickSaveFileAsync();
        if (file is null) return;
        await using var stream = await file.OpenStreamForWriteAsync();
        CreateWorkbook(stream, headers, rows);
#else
        var path = Path.Combine(FileSystem.CacheDirectory, fileName);
        await using var stream = File.Create(path);
        CreateWorkbook(stream, headers, rows);
#endif
    }

    public static async Task<List<string[]>> OpenAsync(string title = "Pilih file Excel")
    {
        var file = await FilePicker.Default.PickAsync(new PickOptions
        {
            PickerTitle = title,
            FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                { DevicePlatform.WinUI, new[] { ".xlsx" } },
                { DevicePlatform.Android, new[] { "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" } },
                { DevicePlatform.iOS, new[] { "com.microsoft.excel.xlsx" } },
                { DevicePlatform.MacCatalyst, new[] { "xlsx" } }
            })
        });
        if (file is null) return new();
        await using var stream = await file.OpenReadAsync();
        return ReadWorkbook(stream);
    }

    private static void CreateWorkbook(Stream output, string[] headers, IEnumerable<string[]> rows)
    {
        using var zip = new ZipArchive(output, ZipArchiveMode.Create, true);
        Write(zip, "[Content_Types].xml", """
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="xml" ContentType="application/xml"/><Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/><Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/><Override PartName="/xl/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"/></Types>
""");
        Write(zip, "_rels/.rels", """
<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/></Relationships>
""");
        Write(zip, "xl/_rels/workbook.xml.rels", """
<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/><Relationship Id="rId2" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/></Relationships>
""");
        Write(zip, "xl/workbook.xml", """
<?xml version="1.0" encoding="UTF-8" standalone="yes"?><workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"><sheets><sheet name="Data" sheetId="1" r:id="rId1"/></sheets></workbook>
""");
        Write(zip, "xl/styles.xml", """
<?xml version="1.0" encoding="UTF-8" standalone="yes"?><styleSheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><fonts count="2"><font><sz val="11"/><name val="Calibri"/></font><font><b/><sz val="11"/><name val="Calibri"/></font></fonts><fills count="2"><fill><patternFill patternType="none"/></fill><fill><patternFill patternType="gray125"/></fill></fills><borders count="1"><border/></borders><cellXfs count="2"><xf numFmtId="0" fontId="0" fillId="0" borderId="0"/><xf numFmtId="0" fontId="1" fillId="0" borderId="0"/></cellXfs></styleSheet>
""");

        var sb = new StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData>");
        AppendRow(sb, 1, headers, 1);
        int rowNo = 2;
        foreach (var row in rows) AppendRow(sb, rowNo++, row, 0);
        sb.Append("</sheetData></worksheet>");
        Write(zip, "xl/worksheets/sheet1.xml", sb.ToString());
    }

    private static void AppendRow(StringBuilder sb, int rowNumber, IReadOnlyList<string> values, int style)
    {
        sb.Append($"<row r=\"{rowNumber}\">");
        for (int i = 0; i < values.Count; i++)
        {
            var cell = ColumnName(i + 1) + rowNumber;
            var value = values[i] ?? string.Empty;
            sb.Append($"<c r=\"{cell}\" t=\"inlineStr\" s=\"{style}\"><is><t>{System.Security.SecurityElement.Escape(value)}</t></is></c>");
        }
        sb.Append("</row>");
    }

    private static List<string[]> ReadWorkbook(Stream stream)
    {
        using var zip = new ZipArchive(stream, ZipArchiveMode.Read, true);
        var sheetEntry = zip.GetEntry("xl/worksheets/sheet1.xml") ?? throw new InvalidDataException("Sheet Excel tidak ditemukan.");
        XDocument sheet;
        using (var s = sheetEntry.Open()) sheet = XDocument.Load(s);
        var ns = XNamespace.Get("http://schemas.openxmlformats.org/spreadsheetml/2006/main");
        var shared = new List<string>();
        var sharedEntry = zip.GetEntry("xl/sharedStrings.xml");
        if (sharedEntry is not null)
        {
            using var s = sharedEntry.Open();
            var doc = XDocument.Load(s);
            foreach (var si in doc.Descendants(ns + "si")) shared.Add(string.Concat(si.Descendants(ns + "t").Select(x => x.Value)));
        }

        var result = new List<string[]>();
        foreach (var row in sheet.Descendants(ns + "row"))
        {
            var cells = row.Elements(ns + "c").ToList();
            if (cells.Count == 0) continue;
            var max = cells.Select(c => ColumnIndex(c.Attribute("r")?.Value ?? "A1")).DefaultIfEmpty(1).Max();
            var values = Enumerable.Repeat(string.Empty, max).ToArray();
            foreach (var c in cells)
            {
                var refText = c.Attribute("r")?.Value ?? "A1";
                var idx = ColumnIndex(refText) - 1;
                var type = c.Attribute("t")?.Value;
                var v = type == "inlineStr" || type == "str"
                    ? string.Concat(c.Descendants(ns + "t").Select(x => x.Value))
                    : c.Element(ns + "v")?.Value ?? string.Empty;
                if (type == "s" && int.TryParse(v, out var si) && si >= 0 && si < shared.Count) v = shared[si];
                // Formula cells may contain the cached value in <v>; string formula
                // cells use <f> + <v>. The code above already handles the cached value.
                if (idx >= 0 && idx < values.Length) values[idx] = v;
            }
            result.Add(values);
        }
        return result;
    }

    private static int ColumnIndex(string cellRef)
    {
        int n = 0;
        foreach (var ch in cellRef.TakeWhile(char.IsLetter)) n = n * 26 + char.ToUpperInvariant(ch) - 'A' + 1;
        return Math.Max(1, n);
    }

    private static string ColumnName(int index)
    {
        var result = "";
        while (index > 0) { index--; result = (char)('A' + index % 26) + result; index /= 26; }
        return result;
    }

    private static void Write(ZipArchive zip, string name, string content)
    {
        var entry = zip.CreateEntry(name, CompressionLevel.Fastest);
        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
        writer.Write(content);
    }
}
