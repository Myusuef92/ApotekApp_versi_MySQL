using MySqlConnector;
using System.Collections.ObjectModel;

namespace ApotekApp;

public partial class ResepPage : ContentPage
{
    readonly ObservableCollection<R> items = new();
    readonly List<Patient> patients = new();
    int? _selectedPatientId;

    public ResepPage()
    {
        InitializeComponent();
        Cv.ItemsSource = items;
        CvPasien.ItemsSource = patients;
        CmbJenis.SelectedIndex = 0;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
    }

    public async Task LoadAsync()
    {
        items.Clear();
        patients.Clear();
        await using var c = new MySqlConnection(MauiProgram.ConnectionString);
        await c.OpenAsync();

        await using (var pc = new MySqlCommand("SELECT id,no_rm,nama FROM pelanggan WHERE aktif=1 ORDER BY nama", c))
        await using (var pr = await pc.ExecuteReaderAsync())
        {
            while (await pr.ReadAsync())
            {
                patients.Add(new Patient(
                    Convert.ToInt32(pr[0]),
                    pr[1]?.ToString() ?? "",
                    pr[2]?.ToString() ?? ""));
            }
        }
        CvPasien.ItemsSource = patients.ToList();

        const string sql = @"
            SELECT r.id,r.no_resep,r.tanggal,r.nama_dokter,r.status,r.jenis_resep,
                   COALESCE(p.nama,'Pasien umum') AS pasien
            FROM resep r
            LEFT JOIN pelanggan p ON p.id=r.pelanggan_id
            ORDER BY r.tanggal DESC LIMIT 500;";
        await using var cmd = new MySqlCommand(sql, c);
        await using var rd = await cmd.ExecuteReaderAsync();
        while (await rd.ReadAsync())
        {
            items.Add(new R
            {
                Id = Convert.ToInt32(rd[0]),
                NoResep = rd[1]?.ToString() ?? "",
                Tanggal = DateTime.TryParse(rd[2]?.ToString(), out var dt) ? dt.ToString("dd/MM/yyyy HH:mm") : "-",
                Dokter = rd[3]?.ToString() ?? "-",
                Status = rd[4]?.ToString() ?? "Menunggu",
                Jenis = rd[5]?.ToString() ?? "Resep Umum",
                Pasien = rd[6]?.ToString() ?? "Pasien umum"
            });
        }
        Apply();
    }

    void Apply()
    {
        var q = TxtSearch.Text?.Trim() ?? "";
        var st = CmbStatus.SelectedItem?.ToString() ?? "Semua Status";
        Cv.ItemsSource = items.Where(x =>
            (string.IsNullOrWhiteSpace(q) || (x.NoResep + x.Pasien + x.Dokter).Contains(q, StringComparison.OrdinalIgnoreCase)) &&
            (st == "Semua Status" || x.Status == st)).ToList();
    }

    void OnSearchChanged(object s, TextChangedEventArgs e) => Apply();
    void OnFilterChanged(object s, EventArgs e) => Apply();
    async void OnRefreshClicked(object s, EventArgs e) => await LoadAsync();

    void OnTambahClicked(object s, EventArgs e)
    {
        _selectedPatientId = null;
        TxtPasien.Text = "";
        TxtDokter.Text = "";
        TxtSIP.Text = "";
        CmbJenis.SelectedIndex = 0;
        TxtCatatan.Text = "";
        LblPasienTerpilih.Text = "Belum ada pasien dipilih";
        LblPasienTerpilih.TextColor = Color.FromArgb("#64748B");
        PasienListBorder.IsVisible = patients.Count > 0;
        CvPasien.ItemsSource = patients.ToList();
        Modal.IsVisible = true;
    }

    void OnBatalClicked(object s, EventArgs e) => Modal.IsVisible = false;

    private async void OnTambahPasienBaruClicked(object sender, EventArgs e)
    {
        Modal.IsVisible = false;
        if (Application.Current?.MainPage is NavigationPage nav && nav.CurrentPage is MainPage main)
            await main.OpenPelangganAsync();
        else if (Application.Current?.MainPage is MainPage directMain)
            await directMain.OpenPelangganAsync();
    }


    void OnPasienFocused(object s, FocusEventArgs e)
    {
        PasienListBorder.IsVisible = true;
        FilterPatients(TxtPasien.Text);
    }

    void OnPasienTextChanged(object s, TextChangedEventArgs e)
    {
        _selectedPatientId = null;
        LblPasienTerpilih.Text = "Pilih pasien dari daftar";
        LblPasienTerpilih.TextColor = Color.FromArgb("#EA580C");
        PasienListBorder.IsVisible = true;
        FilterPatients(e.NewTextValue);
    }

    void FilterPatients(string? query)
    {
        var key = query?.Trim() ?? "";
        CvPasien.ItemsSource = string.IsNullOrWhiteSpace(key)
            ? patients.ToList()
            : patients.Where(p => (p.Nama + p.NoRM).Contains(key, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    void OnPasienSelected(object s, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not Patient p)
            return;
        _selectedPatientId = p.Id;
        TxtPasien.Text = p.Nama;
        LblPasienTerpilih.Text = $"Terpilih: {p.NoRM} • {p.Nama}";
        LblPasienTerpilih.TextColor = Color.FromArgb("#059669");
        PasienListBorder.IsVisible = false;
        CvPasien.SelectedItem = null;
    }

    async void OnSimpanClicked(object s, EventArgs e)
    {
        var doctor = TxtDokter.Text?.Trim() ?? "";
        if (_selectedPatientId is null)
        {
            var typed = TxtPasien.Text?.Trim() ?? "";
            var match = patients.FirstOrDefault(x => x.Nama.Equals(typed, StringComparison.OrdinalIgnoreCase) || x.NoRM.Equals(typed, StringComparison.OrdinalIgnoreCase));
            if (match is not null)
                _selectedPatientId = match.Id;
        }

        if (_selectedPatientId is null)
        {
            await AppDialog.AlertAsync("Validasi", "Pilih nama pasien dari daftar. Jika pasien belum ada, tambahkan terlebih dahulu di menu Data Pelanggan.", "OK");
            return;
        }
        if (string.IsNullOrWhiteSpace(doctor))
        {
            await AppDialog.AlertAsync("Validasi", "Nama dokter / klinik wajib diisi.", "OK");
            return;
        }

        try
        {
            await using var c = new MySqlConnection(MauiProgram.ConnectionString);
            await c.OpenAsync();
            var no = $"RSP-{DateTime.Now:yyyyMMddHHmmssfff}";
            await using var cmd = new MySqlCommand(@"
                INSERT INTO resep(no_resep,tanggal,pelanggan_id,nama_dokter,sip_dokter,status,jenis_resep,catatan,user_id)
                VALUES(@no,@tgl,@pid,@dok,@sip,'Menunggu',@jenis,@cat,@uid);", c);
            cmd.Parameters.AddWithValue("@no", no);
            cmd.Parameters.AddWithValue("@tgl", DateTime.Now);
            cmd.Parameters.AddWithValue("@pid", _selectedPatientId.Value);
            cmd.Parameters.AddWithValue("@dok", doctor);
            cmd.Parameters.AddWithValue("@sip", (object?)TxtSIP.Text?.Trim() ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@jenis", CmbJenis.SelectedItem?.ToString() ?? "Resep Umum");
            cmd.Parameters.AddWithValue("@cat", (object?)TxtCatatan.Text?.Trim() ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@uid", Preferences.Get("CurrentUserId", 0));
            await cmd.ExecuteNonQueryAsync();
            Modal.IsVisible = false;
            await LoadAsync();
            await AppDialog.AlertAsync("Berhasil", $"Resep {no} berhasil disimpan dengan pasien {TxtPasien.Text}.", "OK");
        }
        catch (Exception ex)
        {
            await AppDialog.AlertAsync("Simpan Resep", $"Gagal menyimpan resep:\n{ex.Message}", "OK");
        }
    }

    async void OnSelesaiClicked(object s, EventArgs e)
    {
        if ((s as Button)?.CommandParameter is not int id) return;
        try
        {
            await using var c = new MySqlConnection(MauiProgram.ConnectionString);
            await c.OpenAsync();
            await using var cmd = new MySqlCommand("UPDATE resep SET status='Selesai' WHERE id=@id", c);
            cmd.Parameters.AddWithValue("@id", id);
            await cmd.ExecuteNonQueryAsync();
            await LoadAsync();
        }
        catch (Exception ex)
        {
            await AppDialog.AlertAsync("Resep", $"Gagal memperbarui status:\n{ex.Message}", "OK");
        }
    }

    sealed class R
    {
        public int Id { get; set; }
        public string NoResep { get; set; } = "";
        public string Tanggal { get; set; } = "";
        public string Pasien { get; set; } = "";
        public string Dokter { get; set; } = "";
        public string Status { get; set; } = "";
        public string Jenis { get; set; } = "";
        public bool CanFinish => !string.Equals(Status, "Selesai", StringComparison.OrdinalIgnoreCase);
    }

    sealed record Patient(int Id, string NoRM, string Nama);
}
