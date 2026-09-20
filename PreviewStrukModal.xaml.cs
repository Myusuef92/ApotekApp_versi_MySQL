using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using System;
using System.Collections.Generic;

namespace ApotekApp;

public class StrukItemModel
{
    public string NamaObat { get; set; } = "";
    public int Qty { get; set; }
    public string Harga { get; set; } = "0";
    public string Subtotal { get; set; } = "0";
}

public class StrukDataModel
{
    public string NoNota { get; set; } = "";
    public string Tanggal { get; set; } = "";
    public List<StrukItemModel> Items { get; set; } = new();
    public string Subtotal { get; set; } = "Rp 0";
    public string Diskon { get; set; } = "Rp 0";
    public string Ppn { get; set; } = "Rp 0";
    public string GrandTotal { get; set; } = "Rp 0";
    public string Bayar { get; set; } = "Rp 0";
    public string Kembali { get; set; } = "Rp 0";
}

public partial class PreviewStrukModal : ContentView
{
    private StrukDataModel? _currentData;

    public PreviewStrukModal()
    {
        InitializeComponent();
    }

    public void RenderStrukData(StrukDataModel data)
    {
        _currentData = data;

        // 1. AMBIL HEADER DARI SETTING (Preferences)
        string namaApotek = Preferences.Get("NamaApotek", "APOTEK SEHAT");
        string alamatApotek = Preferences.Get("AlamatApotek", "Jl. Kesehatan No. 1, Jakarta");
        string telpApotek = Preferences.Get("TeleponApotek", "0812-3456-7890");

        LblNamaApotek.Text = string.IsNullOrWhiteSpace(namaApotek) ? "APOTEK SEHAT" : namaApotek;
        LblAlamatApotek.Text = string.IsNullOrWhiteSpace(alamatApotek) ? "-" : alamatApotek;
        LblTelpApotek.Text = $"Telp: {telpApotek}";
// 2. ISIKAN DATA NOTA & KEUANGAN
        LblNoNota.Text = data.NoNota;
        LblTanggal.Text = string.IsNullOrWhiteSpace(data.Tanggal) ? DateTime.Now.ToString("dd/MM/yyyy HH:mm") : data.Tanggal;
        LblSubtotal.Text = data.Subtotal;
        LblDiskon.Text = data.Diskon;
        LblPpn.Text = data.Ppn;
        LblGrandTotal.Text = data.GrandTotal;
        LblBayar.Text = data.Bayar;
        LblKembali.Text = data.Kembali;

        // 3. ISIKAN ITEM LIST KE UI
        ContainerStrukItems.Children.Clear();
        foreach (var item in data.Items)
        {
            var itemLayout = new VerticalStackLayout { Spacing = 2 };
            itemLayout.Children.Add(new Label
            {
                Text = item.NamaObat,
                FontSize = 11,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#111827")
            });

            itemLayout.Children.Add(new Label
            {
                Text = $"{item.Qty} x Rp {item.Harga} = Rp {item.Subtotal}",
                FontSize = 10,
                TextColor = Color.FromArgb("#4B5563")
            });

            ContainerStrukItems.Children.Add(itemLayout);
        }

        this.IsVisible = true;
    }

    private void OnBatalClicked(object sender, EventArgs e)
    {
        this.IsVisible = false;
    }

    private async void OnCetakStrukClicked(object sender, EventArgs e)
    {
        if (_currentData == null) return;

        try
        {
            var items = _currentData.Items.Select(x =>
            {
                _ = int.TryParse(x.Qty.ToString(), out var qty);
                _ = decimal.TryParse(x.Harga, out var harga);
                _ = decimal.TryParse(x.Subtotal, out var subtotal);
                return (x.NamaObat, qty, harga, subtotal);
            });

            var body = Services.BrowserPrintService.ReceiptHtml(
                Preferences.Get("NamaApotek", "APOTEK SEHAT"),
                Preferences.Get("AlamatApotek", ""),
                Preferences.Get("TeleponApotek", ""),
                "",
                _currentData.NoNota,
                DateTime.TryParse(_currentData.Tanggal, out var dt) ? dt : DateTime.Now,
                items,
                ParseMoney(_currentData.Subtotal),
                ParseMoney(_currentData.Diskon),
                ParseMoney(_currentData.Ppn),
                ParseMoney(_currentData.GrandTotal),
                ParseMoney(_currentData.Bayar),
                ParseMoney(_currentData.Kembali),
                "-- Terima Kasih Semoga Lekas Sembuh --",
                "80mm");

            var css = @"
.receipt{width:80mm;max-width:80mm;margin:0 auto;background:#fff;color:#111827;font-size:10px;line-height:1.35;padding:0}
.receipt header{text-align:center}.store{font-size:16px;font-weight:700}
.receipt hr{border:0;border-top:1px dashed #111827;margin:8px 0}
.meta{font-size:10px}.item{margin:5px 0}.item .name{font-weight:700}
.item span,.totals span{float:right}.totals>div{clear:both;overflow:hidden;margin:3px 0}
.grand{font-weight:700;font-size:12px}.change{font-weight:700}.footer{text-align:center;margin-top:10px}
@page{size:80mm auto;margin:0}
@media screen{body{background:#e5e7eb;padding:18px}.receipt{box-shadow:0 2px 10px rgba(0,0,0,.12);padding:3mm}}
@media print{html,body{width:80mm;margin:0;padding:0;background:#fff}.receipt{width:80mm;max-width:80mm;margin:0;padding:0;box-shadow:none}}
";

            var ok = await Services.BrowserPrintService.OpenPrintPreviewAsync(
                $"Struk {_currentData.NoNota}", body, css);
            if (!ok)
            {
                await Application.Current!.MainPage!.DisplayAlert(
                    "Cetak", "Browser tidak ditemukan/gagal dibuka untuk menampilkan Print Preview.", "OK");
                return;
            }

            IsVisible = false;
        }
        catch (Exception ex)
        {
            if (Application.Current?.MainPage != null)
                await Application.Current.MainPage.DisplayAlert("Error Cetak", ex.Message, "OK");
        }
    }

    private static decimal ParseMoney(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return 0;
        var cleaned = new string(value.Where(char.IsDigit).ToArray());
        return decimal.TryParse(cleaned, out var result) ? result : 0;
    }

}