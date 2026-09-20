using ApotekApp.Models;
using Microsoft.EntityFrameworkCore;

namespace ApotekApp.Data;

public partial class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<KategoriObat> KategoriObats => Set<KategoriObat>();
    public DbSet<Obat> Obats => Set<Obat>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Penjualan> Penjualans => Set<Penjualan>();
    public DbSet<DetailPenjualan> DetailPenjualans => Set<DetailPenjualan>();
    public DbSet<Pembelian> Pembelians => Set<Pembelian>();
    public DbSet<DetailPembelian> DetailPembelians => Set<DetailPembelian>();
    public DbSet<Pelanggan> Pelanggans => Set<Pelanggan>();
    public DbSet<Resep> Resep => Set<Resep>();
    public DbSet<MutasiStok> MutasiStoks => Set<MutasiStok>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<User>().ToTable("users");
        modelBuilder.Entity<Role>().ToTable("roles");
        modelBuilder.Entity<Obat>().ToTable("obats");
        modelBuilder.Entity<Supplier>().ToTable("suppliers");
        modelBuilder.Entity<Penjualan>().ToTable("penjualan");
        modelBuilder.Entity<DetailPenjualan>().ToTable("detail_penjualan");
        modelBuilder.Entity<Pembelian>().ToTable("pembelians");
        modelBuilder.Entity<DetailPembelian>().ToTable("detail_pembelians");
        modelBuilder.Entity<Pelanggan>().ToTable("pelanggan");
        modelBuilder.Entity<Resep>().ToTable("resep");
        modelBuilder.Entity<MutasiStok>().ToTable("mutasi_stok");
        // KategoriObat belum menjadi bagian schema database aktif.
        modelBuilder.Ignore<KategoriObat>();
        modelBuilder.Entity<User>().HasIndex(u => u.Username).IsUnique();
        modelBuilder.Entity<Obat>().HasIndex(o => o.KodeObat).IsUnique();
        modelBuilder.Entity<Obat>().Property(o => o.TanggalKadaluarsa).HasColumnName("TanggalKadaluarsa");
    }
}
