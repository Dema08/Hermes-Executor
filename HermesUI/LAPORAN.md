# Laporan Dokumentasi Proyek: Hermes-Executor

Dokumen ini berisi penjelasan lengkap mengenai struktur folder, fungsi folder, serta rincian setiap file yang terdapat di dalam proyek **Hermes-Executor**.

---

## 📁 Struktur Folder Utama

```text
C:\laragon\www\Hermes-Executor
├── 📁 Controls/         <-- Komponen UI kustom yang dapat digunakan kembali (Reusable UI Controls)
├── 📁 Core/             <-- Logika inti aplikasi (Backend, Injection, dan Eksekusi Script)
├── 📁 Resources/        <-- Aset aplikasi (Ikon, Logo, dll.)
├── 📁 Styles/           <-- Definisi tema, warna, dan styling global (XAML ResourceDictionaries)
├── 📁 Views/            <-- Halaman atau panel tampilan tambahan (Views)
├── App.xaml             <-- Entry point aplikasi WPF & definisi ResourceDictionary global
├── App.xaml.cs          <-- Code-behind untuk App.xaml
├── MainWindow.xaml      <-- Desain UI Utama aplikasi (Layout 3 Kolom & Title Bar Kustom)
├── MainWindow.xaml.cs   <-- Logika utama aplikasi (Event handling, Window controls, Console, Timer)
├── Hermes-Executor.csproj <-- File konfigurasi proyek .NET 9.0 WPF
└── README.md            <-- Dokumentasi ringkas proyek
```

---

## 📂 Penjelasan Folder & File

### 1. Root Directory (`C:\laragon\www\Hermes-Executor`)
*   **`Hermes-Executor.csproj`**
    *   **Fungsi**: File proyek utama berbasis XML untuk .NET 9.0 SDK. Mengatur target framework (`net9.0-windows`), konfigurasi WPF, serta NuGet packages yang digunakan (`ModernWpfUI`, `AvalonEdit`, `Newtonsoft.Json`, `FontAwesome.Sharp`).
*   **`App.xaml`**
    *   **Fungsi**: Titik awal (*entry point*) aplikasi WPF. Berisi referensi *merged dictionaries* yang menggabungkan tema global (`DarkTheme.xaml` dan `HermesTheme.xaml`).
*   **`App.xaml.cs`**
    *   **Fungsi**: *Code-behind* untuk `App.xaml` yang menangani inisialisasi level aplikasi.
*   **`MainWindow.xaml`**
    *   **Fungsi**: Berisi antarmuka pengguna (UI) utama aplikasi dengan konsep layout 3 kolom (Sidebar, Script Editor dengan AvalonEdit, dan Console Panel) serta Title Bar kustom beraksen emas.
*   **`MainWindow.xaml.cs`**
    *   **Fungsi**: Mengatur seluruh logika interaksi pada jendela utama, meliputi kontrol jendela (minimize, maximize, close, drag), manajemen console, timer pengecekan proses Roblox, serta event tombol *Inject* dan *Execute*.
*   **`README.md`**
    *   **Fungsi**: Dokumentasi ringkas mengenai spesifikasi teknis dan struktur proyek.

---

### 2. Folder `Core/`
*Berisi kelas-kelas logika inti dan komunikasi backend.*
*   **`Core/Injector.cs`**
    *   **Fungsi**: Mengelola logika deteksi proses Roblox (`RobloxPlayerBeta.exe`) secara asinkron (`Task`) dan menangani proses *injection* serta mengirim pesan status kembali ke Console.
*   **`Core/ScriptEngine.cs`**
    *   **Fungsi**: Mengelola logika eksekusi script Lua yang ditulis oleh pengguna di dalam editor, lengkap dengan validasi dan penanganan feedback eksekusi.

---

### 3. Folder `Styles/`
*Berisi kumpulan ResourceDictionary XAML untuk pengelolaan tema visual.*
*   **`Styles/DarkTheme.xaml`**
    *   **Fungsi**: Mendefinisikan palet warna dasar gelap untuk latar belakang utama (`#0D0D0D`), kontrol (`#1A1A1A`), garis batas, serta warna teks primer dan sekunder.
*   **`Styles/HermesTheme.xaml`**
    *   **Fungsi**: Mendefinisikan palet warna khusus bertema Hermes (Emas: `#FFD700`, Emas Tua: `#C0A000`, Emas Muda: `#FFE44D`, serta warna status hijau, merah, dan kuning).

---

### 4. Folder `Controls/`
*   **Fungsi**: Disediakan untuk menempatkan komponen UI modular kustom (seperti tab script atau indikator status kustom) jika diperlukan pengembangan lanjutan.

---

### 5. Folder `Views/`
*   **Fungsi**: Disediakan untuk menempatkan halaman atau view terpisah (seperti panel pengaturan, editor tambahan, atau tampilan console terpisah).

---

### 6. Folder `Resources/`
*   **Fungsi**: Tempat penyimpanan aset pendukung seperti ikon aplikasi (`Hermes.ico`, `HermesLogo.svg`) yang disematkan ke dalam proyek.
