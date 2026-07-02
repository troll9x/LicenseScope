# WinLic Manager — Release Notes / Ghi Chú Phát Hành
## v1.3-beta2

**Branch:** [`v1.3-beta2`](https://github.com/ardennguyen/WinLic/tree/v1.3-beta2)

---

🇺🇸 [English](#english) · 🇻🇳 [Tiếng Việt](#tiếng-việt)

---

<a id="english"></a>
## 🇺🇸 English

**Release date:** 2026-06-30  
**Platforms:** Windows 10 (1903+) / Windows 11 x64

### What's included

| File | Description |
|---|---|
| `WinLicApp-v1.3-beta2.exe` | Standalone GUI application — run directly, no install needed |
| `WinLicApp-v1.3-beta2.exe.sha256` | SHA-256 hash of the EXE |
| `WinLicApp-v1.3-beta2.zip` | EXE in a ZIP archive |
| `WinLicApp-v1.3-beta2.zip.sha256` | SHA-256 hash of the ZIP |
| `WinLicManager-v1.3-beta2.ps1` | Standalone PowerShell CLI script |
| `WinLicManager-v1.3-beta2.ps1.sha256` | SHA-256 hash of the PS1 |
| `WinLicPS-v1.3-beta2.zip` | PowerShell CLI bundle (`WinLicManager.ps1` + `settings.ini`) |
| `WinLicPS-v1.3-beta2.zip.sha256` | SHA-256 hash of the CLI bundle |

### What's new since v1.3-beta1

#### 🐛 Fix — About dialog version check hyperlink

The "new version available" hyperlink in the About dialog was always linking to the generic
GitHub releases list (`/releases`) regardless of which version was returned by the API.

It now links directly to the specific release tag page:
```
https://github.com/ardennguyen/WinLic/releases/tag/<fetched-tag>
```

This means clicking the version link in the About dialog takes you straight to the correct
release page for the available update.

> This is the only change from v1.3-beta1. The PS1 and CLI ZIP are identical to v1.3-beta1.

### Everything from v1.3-beta1

This release carries all features introduced in v1.3-beta1:

- **9-Layer 3rd-Party Activation Audit** (Option 7) — GVLK, expiry, Online KMS, KMS38, TSforge, SPP event log, legal notice
- **Auto-Update Scan Defaults** — `↻ Update Defaults` (app) / Option `U` (CLI)
- **Backup Key Management** — mismatch detection with admin/non-admin handling
- **Version Check in About** — live GitHub API check with clickable update link *(fixed in this release)*
- **Log restore ordering** fix — previous session log appears above "Ready" on elevation relaunch
- **Version label** fix — top-left label always in sync with `Localization.cs`

See [v1.3-beta1 release notes](../v1.3-beta1/RELEASE_NOTES_v1.3-beta1.md) for the full changelog.

### Upgrading from v1.3-beta1

Replace `WinLicApp.exe` only — no other files changed.

### Checksums

```
821A7DE3BAAF9B613571FB98AF9EBB13BCD77262E5C7947F2942BE3F66A91EB2  WinLicApp-v1.3-beta2.exe
A5CE410FAF79E192F80CC76572BE8A7D03D9D776CDAE3038B0F3FE9357FE1504  WinLicApp-v1.3-beta2.zip
9F2FEA821262C9CE7C61B2A330D9929FE315E328A588BE35E70AE98EB023C95F  WinLicManager-v1.3-beta2.ps1
DCDFACD1A65A64AF8F11DF557329C0E3AC143D9F00C6EF4A2F2B6B82751B3DF9  WinLicPS-v1.3-beta2.zip
```

**Verify (PowerShell):**
```powershell
Get-FileHash .\WinLicApp-v1.3-beta2.zip     -Algorithm SHA256
Get-FileHash .\WinLicPS-v1.3-beta2.zip      -Algorithm SHA256
Get-FileHash .\WinLicManager-v1.3-beta2.ps1 -Algorithm SHA256
```

---

<a id="tiếng-việt"></a>
## 🇻🇳 Tiếng Việt

**Ngày phát hành:** 2026-06-30  
**Nền tảng:** Windows 10 (1903+) / Windows 11 x64

### Nội dung bản phát hành

| File | Mô tả |
|---|---|
| `WinLicApp-v1.3-beta2.exe` | Ứng dụng GUI độc lập — chạy trực tiếp, không cần cài đặt |
| `WinLicApp-v1.3-beta2.exe.sha256` | Mã băm SHA-256 của file EXE |
| `WinLicApp-v1.3-beta2.zip` | EXE đóng gói trong file ZIP |
| `WinLicApp-v1.3-beta2.zip.sha256` | Mã băm SHA-256 của file ZIP |
| `WinLicManager-v1.3-beta2.ps1` | Script PowerShell CLI độc lập |
| `WinLicManager-v1.3-beta2.ps1.sha256` | Mã băm SHA-256 của file PS1 |
| `WinLicPS-v1.3-beta2.zip` | Bộ công cụ CLI (`WinLicManager.ps1` + `settings.ini`) |
| `WinLicPS-v1.3-beta2.zip.sha256` | Mã băm SHA-256 của bộ cài CLI |

### Điểm mới so với v1.3-beta1

#### 🐛 Sửa lỗi — Hyperlink kiểm tra phiên bản trong hộp thoại Giới thiệu

Hyperlink "có phiên bản mới" trong hộp thoại Giới thiệu luôn dẫn đến trang danh sách releases
chung (`/releases`) bất kể phiên bản nào được trả về từ API GitHub.

Nay được sửa để dẫn trực tiếp đến trang release của tag cụ thể:
```
https://github.com/ardennguyen/WinLic/releases/tag/<tag-được-lấy>
```

Nhấn vào hyperlink trong hộp thoại Giới thiệu sẽ đưa thẳng đến trang release đúng của bản cập nhật.

> Đây là thay đổi duy nhất so với v1.3-beta1. File PS1 và CLI ZIP giống hệt v1.3-beta1.

### Tất cả tính năng từ v1.3-beta1

Bản phát hành này kế thừa toàn bộ tính năng từ v1.3-beta1:

- **Kiểm Tra Kích Hoạt Bên Thứ Ba 9 Lớp** (Tùy chọn 7) — GVLK, hết hạn, Online KMS, KMS38, TSforge, nhật ký SPP, thông báo pháp lý
- **Tự động Cập Nhật Mặc Định Quét** — nút `↻ Cập nhật mặc định` (ứng dụng) / Tùy chọn `U` (CLI)
- **Quản Lý Key Dự Phòng** — phát hiện không khớp với xử lý admin/non-admin
- **Kiểm Tra Phiên Bản trong Giới Thiệu** — kiểm tra GitHub API trực tiếp với hyperlink cập nhật *(đã sửa trong bản này)*
- Sửa lỗi **thứ tự khôi phục nhật ký** — nhật ký phiên trước hiển thị trên thông báo "Sẵn sàng"
- Sửa lỗi **nhãn phiên bản** — nhãn góc trái luôn đồng bộ với `Localization.cs`

Xem [ghi chú phát hành v1.3-beta1](../v1.3-beta1/RELEASE_NOTES_v1.3-beta1.md) để biết đầy đủ thay đổi.

### Nâng cấp từ v1.3-beta1

Chỉ cần thay thế `WinLicApp.exe` — không có file nào khác thay đổi.

### Mã Kiểm Tra Toàn Vẹn

```
821A7DE3BAAF9B613571FB98AF9EBB13BCD77262E5C7947F2942BE3F66A91EB2  WinLicApp-v1.3-beta2.exe
A5CE410FAF79E192F80CC76572BE8A7D03D9D776CDAE3038B0F3FE9357FE1504  WinLicApp-v1.3-beta2.zip
9F2FEA821262C9CE7C61B2A330D9929FE315E328A588BE35E70AE98EB023C95F  WinLicManager-v1.3-beta2.ps1
DCDFACD1A65A64AF8F11DF557329C0E3AC143D9F00C6EF4A2F2B6B82751B3DF9  WinLicPS-v1.3-beta2.zip
```

**Xác minh (PowerShell):**
```powershell
Get-FileHash .\WinLicApp-v1.3-beta2.zip     -Algorithm SHA256
Get-FileHash .\WinLicPS-v1.3-beta2.zip      -Algorithm SHA256
Get-FileHash .\WinLicManager-v1.3-beta2.ps1 -Algorithm SHA256
```
