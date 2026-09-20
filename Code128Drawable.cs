using Microsoft.Maui.Graphics;

namespace ApotekApp;

public sealed class Code128Drawable : IDrawable
{
    // Code 128 patterns: values 0..106, each pattern is 11 modules except stop (13).
    private static readonly string[] Patterns =
    {
        "212222","222122","222221","121223","121322","131222","122213","122312","132212","221213","221312","231212","112232","122132","122231","113222","123122","123221","223211","221132","221231","213212","223112","312131","311222","321122","321221","312212","322112","322211","212123","212321","232121","111323","131123","131321","112313","132113","132311","211313","231113","231311","112133","112331","132131","113123","113321","133121","313121","211331","231131","213113","213311","213131","311123","311321","331121","312113","312311","332111","314111","221411","431111","111224","111422","121124","121421","141122","141221","112214","112412","122114","122411","142112","142211","241211","221114","413111","241112","134111","111242","121142","121241","114212","124112","124211","411212","421112","421211","212141","214121","412121","111143","111341","131141","114113","114311","411113","411311","113141","114131","311141","411131","211412","211214","211232","2331112"
    };

    private readonly string _value;
    public Code128Drawable(string value) => _value = value ?? string.Empty;

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        canvas.FillColor = Colors.White;
        canvas.FillRectangle(dirtyRect);
        if (string.IsNullOrWhiteSpace(_value)) return;

        var codes = new List<int> { 104 }; // Code 128B start
        foreach (var ch in _value)
        {
            if (ch < 32 || ch > 126) continue;
            codes.Add(ch - 32);
        }
        var checksum = 104;
        for (var i = 1; i < codes.Count; i++) checksum += codes[i] * i;
        codes.Add(checksum % 103);
        codes.Add(106);

        var modules = codes.Sum(c => c == 106 ? 13 : 11);
        var quiet = 10;
        var availableWidth = Math.Max(100, dirtyRect.Width - 20);
        var moduleWidth = Math.Min(3f, availableWidth / (modules + quiet * 2));
        var totalWidth = (modules + quiet * 2) * moduleWidth;
        var x = dirtyRect.X + Math.Max(0, (dirtyRect.Width - totalWidth) / 2) + quiet * moduleWidth;
        var y = dirtyRect.Y + 8;
        var h = Math.Max(50, dirtyRect.Height - 16);

        canvas.FillColor = Colors.Black;
        foreach (var code in codes)
        {
            var pattern = Patterns[code];
            var black = true;
            foreach (var ch in pattern)
            {
                var width = (ch - '0') * moduleWidth;
                if (black) canvas.FillRectangle(x, y, width, h);
                x += width;
                black = !black;
            }
        }
    }
}
