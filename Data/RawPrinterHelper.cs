using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Runtime.Versioning;

namespace ApotekApp;

public class RawPrinterHelper
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public class DOCINFOA
    {
        [MarshalAs(UnmanagedType.LPStr)]
        public string pDocName = "Struk Apotek";
        [MarshalAs(UnmanagedType.LPStr)]
        public string pOutputFile = "";
        [MarshalAs(UnmanagedType.LPStr)]
        public string pDataType = "RAW";
    }

    [DllImport("winspool.Drv", EntryPoint = "OpenPrinterA", SetLastError = true, CharSet = CharSet.Ansi, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
    public static extern bool OpenPrinter([MarshalAs(UnmanagedType.LPStr)] string szPrinter, out IntPtr hPrinter, IntPtr pd);

    [DllImport("winspool.Drv", EntryPoint = "ClosePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
    public static extern bool ClosePrinter(IntPtr hPrinter);

    [DllImport("winspool.Drv", EntryPoint = "StartDocPrinterA", SetLastError = true, CharSet = CharSet.Ansi, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
    public static extern bool StartDocPrinter(IntPtr hPrinter, Int32 level, [In, MarshalAs(UnmanagedType.LPStruct)] DOCINFOA di);

    [DllImport("winspool.Drv", EntryPoint = "EndDocPrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
    public static extern bool EndDocPrinter(IntPtr hPrinter);

    [DllImport("winspool.Drv", EntryPoint = "StartPagePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
    public static extern bool StartPagePrinter(IntPtr hPrinter);

    [DllImport("winspool.Drv", EntryPoint = "EndPagePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
    public static extern bool EndPagePrinter(IntPtr hPrinter);

    [DllImport("winspool.Drv", EntryPoint = "WritePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
    public static extern bool WritePrinter(IntPtr hPrinter, IntPtr pBytes, Int32 dwCount, out Int32 dwWritten);

    public static bool SendBytesToPrinter(string szPrinterName, byte[] bytes)
    {
        IntPtr hPrinter = new IntPtr(0);
        DOCINFOA di = new DOCINFOA();
        bool bSuccess = false;

        if (OpenPrinter(szPrinterName.Normalize(), out hPrinter, IntPtr.Zero))
        {
            if (StartDocPrinter(hPrinter, 1, di))
            {
                if (StartPagePrinter(hPrinter))
                {
                    IntPtr pUnmanagedBytes = Marshal.AllocCoTaskMem(bytes.Length);
                    Marshal.Copy(bytes, 0, pUnmanagedBytes, bytes.Length);
                    bSuccess = WritePrinter(hPrinter, pUnmanagedBytes, bytes.Length, out _);
                    Marshal.FreeCoTaskMem(pUnmanagedBytes);
                    EndPagePrinter(hPrinter);
                }
                EndDocPrinter(hPrinter);
            }
            ClosePrinter(hPrinter);
        }
        return bSuccess;
    }

    public static bool PrintStrukThermal(string namaPrinter, string textContent)
    {
        // 1. Kode ESC/POS
        byte[] initPrinter = new byte[] { 0x1B, 0x40 }; // ESC @ (Reset/Init)
        byte[] feedAndCut = new byte[] { 0x1D, 0x56, 0x42, 0x03 }; // GS V 66 3 (Feed 3 baris & Potong Kertas)

        // 2. Encoding Teks
        byte[] bodyBytes = Encoding.ASCII.GetBytes(textContent + "\n\n\n");

        // 3. Gabungkan byte
        byte[] fullBytes = new byte[initPrinter.Length + bodyBytes.Length + feedAndCut.Length];
        Buffer.BlockCopy(initPrinter, 0, fullBytes, 0, initPrinter.Length);
        Buffer.BlockCopy(bodyBytes, 0, fullBytes, initPrinter.Length, bodyBytes.Length);
        Buffer.BlockCopy(feedAndCut, 0, fullBytes, initPrinter.Length + bodyBytes.Length, feedAndCut.Length);

        return SendBytesToPrinter(namaPrinter, fullBytes);
    }
#if WINDOWS
    [DllImport("winspool.Drv", EntryPoint = "GetDefaultPrinterA", SetLastError = true, CharSet = CharSet.Ansi)]
    private static extern bool GetDefaultPrinter(StringBuilder pszBuffer, ref int pcchBuffer);

    public static string GetDefaultPrinterName()
    {
        var size = 0;
        GetDefaultPrinter(null!, ref size);
        if (size <= 0) return string.Empty;
        var buffer = new StringBuilder(size);
        return GetDefaultPrinter(buffer, ref size) ? buffer.ToString() : string.Empty;
    }

    public static bool PrintBarcodeCode128(string barcode, string productName, string code, int copies = 1)
    {
        if (string.IsNullOrWhiteSpace(barcode) || barcode.Length > 250) return false;
        var printer = GetDefaultPrinterName();
        if (string.IsNullOrWhiteSpace(printer)) return false;

        var init = new byte[] { 0x1B, 0x40 };
        var center = new byte[] { 0x1B, 0x61, 0x01 };
        var hri = new byte[] { 0x1D, 0x48, 0x02 };
        var height = new byte[] { 0x1D, 0x68, 0x50 };
        var width = new byte[] { 0x1D, 0x77, 0x02 };
        var barcodeData = Encoding.ASCII.GetBytes("{B" + barcode);
        var command = new byte[4 + barcodeData.Length];
        command[0] = 0x1D; command[1] = 0x6B; command[2] = 0x49; command[3] = (byte)barcodeData.Length;
        Buffer.BlockCopy(barcodeData, 0, command, 4, barcodeData.Length);

        var text = Encoding.ASCII.GetBytes((productName + "\n" + code + "\n"));
        var feedCut = new byte[] { 0x0A, 0x0A, 0x1D, 0x56, 0x42, 0x03 };
        using var ms = new MemoryStream();
        ms.Write(init); ms.Write(center); ms.Write(text); ms.Write(hri); ms.Write(height); ms.Write(width);
        for (var i = 0; i < Math.Max(1, copies); i++) ms.Write(command);
        ms.Write(feedCut);
        return SendBytesToPrinter(printer, ms.ToArray());
    }
#endif

}