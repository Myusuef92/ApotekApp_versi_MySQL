-- ==============================================================
-- APOTEK APP - MySQL / MariaDB database for phpMyAdmin
-- Database : apotek_db
-- Import this file from phpMyAdmin -> Import.
-- ==============================================================

CREATE DATABASE IF NOT EXISTS `apotek_db`
  CHARACTER SET utf8mb4
  COLLATE utf8mb4_unicode_ci;
USE `apotek_db`;

SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS=0;

CREATE TABLE IF NOT EXISTS `roles` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `nama_role` VARCHAR(100) NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `uq_roles_nama_role` (`nama_role`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `users` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `username` VARCHAR(100) NOT NULL,
  `password` TEXT NOT NULL,
  `nama_lengkap` VARCHAR(150) NOT NULL,
  `role_id` INT NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `uq_users_username` (`username`),
  KEY `idx_users_role` (`role_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `obats` (
  `Id` INT NOT NULL AUTO_INCREMENT,
  `KodeObat` VARCHAR(100) NOT NULL,
  `NamaObat` VARCHAR(255) NOT NULL,
  `Barcode` VARCHAR(100) NULL,
  `Satuan` VARCHAR(50) NOT NULL DEFAULT 'Pcs',
  `Kategori` VARCHAR(100) NOT NULL DEFAULT 'Umum',
  `LokasiRak` VARCHAR(100) NOT NULL DEFAULT '-',
  `HargaBeli` DECIMAL(18,2) NOT NULL DEFAULT 0,
  `HargaJual` DECIMAL(18,2) NOT NULL DEFAULT 0,
  `Stok` INT NOT NULL DEFAULT 0,
  `StokMin` INT NOT NULL DEFAULT 0,
  `TanggalKadaluarsa` VARCHAR(50) NULL,
  `Gambar` LONGTEXT NULL,
  `KodeKFA` VARCHAR(100) NULL,
  `Golongan` VARCHAR(100) NULL,
  `BentukSediaan` VARCHAR(100) NULL,
  `Kekuatan` VARCHAR(100) NULL,
  `Dosis` VARCHAR(100) NULL,
  `IsAktif` TINYINT(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `uq_obats_kode` (`KodeObat`),
  KEY `idx_obats_nama` (`NamaObat`),
  KEY `idx_obats_barcode` (`Barcode`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `suppliers` (
  `Id` INT NOT NULL AUTO_INCREMENT,
  `NamaSupplier` VARCHAR(200) NOT NULL,
  `Telepon` VARCHAR(50) NOT NULL DEFAULT '',
  `Alamat` TEXT NOT NULL,
  `Email` VARCHAR(150) NOT NULL DEFAULT '',
  PRIMARY KEY (`Id`),
  KEY `idx_suppliers_nama` (`NamaSupplier`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `penjualan` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `no_nota` VARCHAR(100) NOT NULL,
  `tanggal` DATETIME NOT NULL,
  `total` DECIMAL(18,2) NOT NULL DEFAULT 0,
  `diskon` DECIMAL(18,2) NOT NULL DEFAULT 0,
  `grand_total` DECIMAL(18,2) NOT NULL DEFAULT 0,
  `bayar` DECIMAL(18,2) NOT NULL DEFAULT 0,
  `kembali` DECIMAL(18,2) NOT NULL DEFAULT 0,
  `user_id` INT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `uq_penjualan_no_nota` (`no_nota`),
  KEY `idx_penjualan_tanggal` (`tanggal`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `detail_penjualan` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `no_nota` VARCHAR(100) NOT NULL,
  `id_obat` INT NOT NULL,
  `nama_obat` VARCHAR(255) NULL,
  `harga` DECIMAL(18,2) NOT NULL,
  `harga_beli` DECIMAL(18,2) NOT NULL DEFAULT 0,
  `qty` INT NOT NULL,
  `subtotal` DECIMAL(18,2) NOT NULL,
  PRIMARY KEY (`id`),
  KEY `idx_detail_penjualan_nota` (`no_nota`),
  KEY `idx_detail_penjualan_obat` (`id_obat`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `pembelians` (
  `IdPembelian` INT NOT NULL AUTO_INCREMENT,
  `NoFaktur` VARCHAR(100) NOT NULL,
  `TanggalPembelian` DATETIME NOT NULL,
  `TotalHarga` DECIMAL(18,2) NOT NULL DEFAULT 0,
  `Catatan` TEXT NULL,
  `SupplierId` INT NOT NULL,
  `nama_supplier` VARCHAR(200) NULL,
  PRIMARY KEY (`IdPembelian`),
  KEY `idx_pembelian_tanggal` (`TanggalPembelian`),
  KEY `idx_pembelian_supplier` (`SupplierId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `detail_pembelians` (
  `IdDetailPembelian` INT NOT NULL AUTO_INCREMENT,
  `PembelianId` INT NOT NULL,
  `ObatId` INT NOT NULL,
  `nama_obat` VARCHAR(255) NULL,
  `Jumlah` INT NOT NULL,
  `HargaBeli` DECIMAL(18,2) NOT NULL DEFAULT 0,
  PRIMARY KEY (`IdDetailPembelian`),
  KEY `idx_detail_pembelian_pembelian` (`PembelianId`),
  KEY `idx_detail_pembelian_obat` (`ObatId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `pengaturan` (
  `id` INT NOT NULL,
  `nama_apotek` VARCHAR(200) NOT NULL DEFAULT 'APOTEK SEHAT',
  `nama_pemilik` VARCHAR(200) NULL,
  `alamat` TEXT NULL,
  `no_telepon` VARCHAR(50) NULL,
  `sia_sipa` VARCHAR(150) NULL,
  `catatan_struk` TEXT NULL,
  `logo_path` TEXT NULL,
  `logo_data` LONGBLOB NULL,
  `ukuran_kertas` VARCHAR(20) NOT NULL DEFAULT '58mm',
  `pajak_ppn` DECIMAL(8,2) NOT NULL DEFAULT 0,
  `organization_id` VARCHAR(150) NULL,
  `satusehat_client_id` VARCHAR(200) NULL,
  `satusehat_client_secret` TEXT NULL,
  `backup_folder` TEXT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `arsip_obat` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `obat_id_lama` INT NOT NULL,
  `kode_obat` VARCHAR(100) NULL,
  `nama_obat` VARCHAR(255) NULL,
  `barcode` VARCHAR(100) NULL,
  `satuan` VARCHAR(50) NULL,
  `kategori` VARCHAR(100) NULL,
  `lokasi_rak` VARCHAR(100) NULL,
  `harga_beli` DECIMAL(18,2) NULL,
  `harga_jual` DECIMAL(18,2) NULL,
  `stok` INT NULL,
  `stok_min` INT NULL,
  `tanggal_kadaluarsa` VARCHAR(50) NULL,
  `diarsipkan_pada` DATETIME NOT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `aset_modal` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `nama` VARCHAR(200) NOT NULL,
  `jenis` VARCHAR(100) NOT NULL,
  `nilai` DECIMAL(18,2) NOT NULL,
  `tanggal` DATETIME NOT NULL,
  `keterangan` TEXT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `pelanggan` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `no_rm` VARCHAR(100) NOT NULL,
  `nama` VARCHAR(200) NOT NULL,
  `nik` VARCHAR(50) NULL,
  `jenis_kelamin` VARCHAR(30) NULL,
  `tanggal_lahir` VARCHAR(50) NULL,
  `telepon` VARCHAR(50) NULL,
  `alamat` TEXT NULL,
  `alergi` TEXT NULL,
  `catatan` TEXT NULL,
  `satusehat_id` VARCHAR(150) NULL,
  `aktif` TINYINT(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`id`),
  UNIQUE KEY `uq_pelanggan_rm` (`no_rm`),
  KEY `idx_pelanggan_nama` (`nama`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `resep` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `no_resep` VARCHAR(100) NOT NULL,
  `tanggal` DATETIME NOT NULL,
  `pelanggan_id` INT NULL,
  `nama_dokter` VARCHAR(200) NULL,
  `sip_dokter` VARCHAR(100) NULL,
  `status` VARCHAR(50) NOT NULL DEFAULT 'Menunggu',
  `jenis_resep` VARCHAR(100) NOT NULL DEFAULT 'Resep Umum',
  `catatan` TEXT NULL,
  `user_id` INT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `uq_resep_no` (`no_resep`),
  KEY `idx_resep_tanggal` (`tanggal`),
  KEY `idx_resep_pelanggan` (`pelanggan_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `resep_detail` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `resep_id` INT NOT NULL,
  `obat_id` INT NULL,
  `nama_obat` VARCHAR(255) NOT NULL,
  `qty` INT NOT NULL DEFAULT 1,
  `aturan_pakai` VARCHAR(255) NULL,
  `dosis` VARCHAR(100) NULL,
  `catatan` TEXT NULL,
  `substitusi` TINYINT(1) NOT NULL DEFAULT 0,
  PRIMARY KEY (`id`),
  KEY `idx_resep_detail_resep` (`resep_id`),
  KEY `idx_resep_detail_obat` (`obat_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `mutasi_stok` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `tanggal` DATETIME NOT NULL,
  `obat_id` INT NOT NULL,
  `jenis` VARCHAR(50) NOT NULL,
  `qty_masuk` INT NOT NULL DEFAULT 0,
  `qty_keluar` INT NOT NULL DEFAULT 0,
  `stok_setelah` INT NOT NULL DEFAULT 0,
  `referensi` VARCHAR(200) NULL,
  `batch` VARCHAR(100) NULL,
  `expired` VARCHAR(50) NULL,
  `harga_satuan` DECIMAL(18,2) NOT NULL DEFAULT 0,
  `user_id` INT NULL,
  `keterangan` TEXT NULL,
  PRIMARY KEY (`id`),
  KEY `idx_mutasi_tanggal` (`tanggal`),
  KEY `idx_mutasi_obat` (`obat_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `audit_log` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `waktu` DATETIME NOT NULL,
  `user_id` INT NULL,
  `modul` VARCHAR(100) NOT NULL,
  `aksi` VARCHAR(100) NOT NULL,
  `referensi` VARCHAR(200) NULL,
  `detail` TEXT NULL,
  PRIMARY KEY (`id`),
  KEY `idx_audit_waktu` (`waktu`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `sesi_kasir` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `user_id` INT NOT NULL,
  `buka` DATETIME NOT NULL,
  `tutup` DATETIME NULL,
  `saldo_awal` DECIMAL(18,2) NOT NULL DEFAULT 0,
  `saldo_akhir` DECIMAL(18,2) NULL,
  `status` VARCHAR(30) NOT NULL DEFAULT 'OPEN',
  `catatan` TEXT NULL,
  PRIMARY KEY (`id`),
  KEY `idx_sesi_user` (`user_id`),
  KEY `idx_sesi_status` (`status`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `batch_obat` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `obat_id` INT NOT NULL,
  `no_batch` VARCHAR(100) NOT NULL,
  `tanggal_kadaluarsa` VARCHAR(50) NULL,
  `qty` INT NOT NULL DEFAULT 0,
  `harga_beli` DECIMAL(18,2) NOT NULL DEFAULT 0,
  `supplier_id` INT NULL,
  PRIMARY KEY (`id`),
  KEY `idx_batch_obat` (`obat_id`),
  KEY `idx_batch_supplier` (`supplier_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

INSERT IGNORE INTO `roles` (`id`,`nama_role`) VALUES
(1,'Admin'),(2,'Apoteker'),(3,'Kasir');

INSERT IGNORE INTO `users` (`id`,`username`,`password`,`nama_lengkap`,`role_id`) VALUES
(1,'admin','$PBKDF2-SHA256$120000$kNB7OAelShFbo+PGGcBIyA==$E/x5yXC92aG15Cz49cgy4dc3JgmFU3fPnNtPXJcRKbs=','Administrator',1);

INSERT IGNORE INTO `pengaturan` (`id`,`nama_apotek`,`alamat`,`no_telepon`,`ukuran_kertas`,`pajak_ppn`) VALUES
(1,'APOTEK SEHAT','Jl. Kesehatan No. 1','0812-3456-7890','58mm',0);

SET FOREIGN_KEY_CHECKS=1;
