# WinLic Manager — Release Notes / Ghi Chú Phát Hành
## v1.3-beta1

**Branch:** [`v1.3-beta1`](https://github.com/ardennguyen/WinLic/tree/v1.3-beta1)

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
| `WinLicApp-v1.3-beta1.exe` | Standalone GUI application — run directly, no install needed |
| `WinLicApp-v1.3-beta1.exe.sha256` | SHA-256 hash of the EXE |
| `WinLicApp-v1.3-beta1.zip` | EXE in a ZIP archive |
| `WinLicApp-v1.3-beta1.zip.sha256` | SHA-256 hash of the ZIP |
| `WinLicManager-v1.3-beta1.ps1` | Standalone PowerShell CLI script |
| `WinLicManager-v1.3-beta1.ps1.sha256` | SHA-256 hash of the PS1 |
| `WinLicPS-v1.3-beta1.zip` | PowerShell CLI bundle (`WinLicManager.ps1` + `settings.ini`) |
| `WinLicPS-v1.3-beta1.zip.sha256` | SHA-256 hash of the CLI bundle |

### New in this release

#### 🔍 9-Layer 3rd-Party Activation Audit (Option 7)

A completely rebuilt detection engine replaces the previous scan.
Each layer targets a different activation method and reports independently:

| Layer | Target | Method |
|-------|--------|--------|
| 1 | **GVLK key** | WMI `PartialProductKey` → match against 30 known Volume/Setup keys; permanent + GVLK = piracy indicator |
| 2 | **Grace period / expiry** | `GracePeriodRemaining` → classifies KMS38 (≥ 2037), TSforge (≥ 2100), Online KMS (165–195 day window) |
| 3 | **Online KMS artifacts** | Scheduled tasks, services, processes, bogus IP `10.0.0.10`, WoW64 registry paths, Office KMS registry |
| 4 | **KMS38 artifacts** | `GenuineTicket.xml`, `gatherosstate.exe`, `clipup.exe` |
| 5 | **TSforge timestamp** | `data.dat` write time vs. Windows install date + Event ID 19 (Windows Update) correlation |
| 6 | **SPP Security Event Log** | Event IDs 12288/12289/12290/8198 — flags external KMS server addresses from event 12290 |
| 7 | **Legal / limitations notice** | Printed at end of scan; documents known false-positive cases |

> **Note:** HWID activation (MAS) creates a genuine Microsoft Digital License — it **cannot be detected** by any local scan. This is documented in the audit output.

#### ⚙️ Auto-Update Scan Defaults

- **App:** Settings dialog → `↻ Update Defaults` — pulls `settings.default.ini` from GitHub
- **CLI:** Main menu option `U`
- Hardcoded fallback of 30 GVLK suffixes and 14 KMS piracy domains when network is unavailable
- `settings.ini` split into `[Default]` (auto-managed) and `[User]` (never overwritten) sections

#### 🔑 Backup Key Management (Option 3)

- Detects mismatch between `BackupProductKeyDefault` (registry) and the active partial key
- **Admin:** Confirmation dialog offers to remove the stale backup key
- **Non-admin:** Displays `⚠ Run as Administrator to remove the stale backup key.` in log output
- DE (Digital Entitlement) systems: mismatch correctly annotated as expected/informational

#### ℹ️ Version Check in About Dialog

- Opens with `Checking for updates…`
- Queries `api.github.com/repos/ardennguyen/WinLic/releases/latest` with 8-second timeout
- **Up to date:** `✔ You are on the latest version.` (green)
- **New version:** `↑ New version available: vX.X` — clickable hyperlink to the Releases page

### Bug Fixes

| Fix | Description |
|-----|-------------|
| Log restore ordering | Session log restored before UAC elevation now appears **above** the "Ready" message |
| Version label | Top-left `v1.0 (beta)` was stale and hardcoded — now driven from `Localization.cs` at all times |
| Non-admin backup key | No output was shown on key mismatch without admin — now shows the `⚠ Run as Administrator` warning |
| Update Defaults button | Label shortened to `↻ Update Defaults`; width reduced 160 → 110 px to prevent log area overlap |
| SPP event log Layer 6 (PS1) | External server address now correctly extracted from event 12290 message body |
| WoW64 KMS registry (PS1) | Added `HKLM:\SOFTWARE\WOW6432Node\...\SoftwareProtectionPlatform` as secondary scan path |
| InstallDate comparison (PS1) | `HKLM:\...\CurrentVersion\InstallDate` read as Unix epoch and compared against `data.dat` write time |
| `clipup.exe` scan (PS1) | Added to process scan list for KMS38 detection |

### Known limitations

- **MAS HWID** activations cannot be detected — they create a genuine Digital Entitlement via Microsoft's API
- Tools **fully removed after activation** leave no detectable traces
- **Windows Loader** (SLIC/BIOS patching) is not reliably detected
- Legitimate enterprise KMS servers may trigger GVLK layer warnings — context matters

### Upgrading from v1.0-beta2

1. Replace `WinLicApp.exe` with the new build — no installer needed
2. Replace `WinLicManager.ps1` — `settings.ini` format is unchanged; `[User]` section is preserved
3. Optionally click `↻ Update Defaults` (app) or press `U` (CLI) to sync the latest scan lists

### Checksums

```
F9AA759E88C3320FB2F3F5BDDCB2B1189FDDCEA3D7199E2D2E40BC3971B9BA3C  WinLicApp-v1.3-beta1.exe
4DB25DBEB92281359E45527E81876ACC26B28C699CAA47B660922642335000B5  WinLicApp-v1.3-beta1.zip
77AED3AA07B4537DA75B6C270953391F6FEAE140E56DF1F8A5652721048FEF08  WinLicManager-v1.3-beta1.ps1
5E9A2CE9DA9A4DCFABE64F2DEFCDE892BE7EEEB6F60D5380B647323835454A65  WinLicPS-v1.3-beta1.zip
```

**Verify (PowerShell):**
```powershell
Get-FileHash .\WinLicApp-v1.3-beta1.zip    -Algorithm SHA256
Get-FileHash .\WinLicPS-v1.3-beta1.zip     -Algorithm SHA256
Get-FileHash .\WinLicManager-v1.3-beta1.ps1 -Algorithm SHA256
```

---

<a id="tiếng-việt"></a>
## 🇻🇳 Tiếng Việt

**Ngày phát hành:** 2026-06-30  
**Nền tảng:** Windows 10 (1903+) / Windows 11 x64

### Nội dung bản phát hành

| File | Mô tả |
|---|---|
| `WinLicApp-v1.3-beta1.exe` | Ứng dụng GUI độc lập — chạy trực tiếp, không cần cài đặt |
| `WinLicApp-v1.3-beta1.exe.sha256` | Mã băm SHA-256 của file EXE |
| `WinLicApp-v1.3-beta1.zip` | EXE đóng gói trong file ZIP |
| `WinLicApp-v1.3-beta1.zip.sha256` | Mã băm SHA-256 của file ZIP |
| `WinLicManager-v1.3-beta1.ps1` | Script PowerShell CLI độc lập |
| `WinLicManager-v1.3-beta1.ps1.sha256` | Mã băm SHA-256 của file PS1 |
| `WinLicPS-v1.3-beta1.zip` | Bộ công cụ CLI (`WinLicManager.ps1` + `settings.ini`) |
| `WinLicPS-v1.3-beta1.zip.sha256` | Mã băm SHA-256 của bộ cài CLI |

### Điểm mới trong phiên bản này

#### 🔍 Kiểm Tra Kích Hoạt Bên Thứ Ba 9 Lớp (Tùy chọn 7)

Hệ thống phát hiện được xây dựng lại hoàn toàn, thay thế bản quét đơn lớp trước đó.
Mỗi lớp nhắm vào một phương pháp kích hoạt khác nhau và báo cáo độc lập:

| Lớp | Mục tiêu | Phương pháp |
|-----|----------|-------------|
| 1 | **Key GVLK** | WMI `PartialProductKey` → đối chiếu với 30 key Volume/Setup đã biết; GVLK vĩnh viễn = dấu hiệu vi phạm |
| 2 | **Thời gian ân hạn / hết hạn** | `GracePeriodRemaining` → phân loại KMS38 (≥ 2037), TSforge (≥ 2100), Online KMS (165–195 ngày) |
| 3 | **Dấu vết Online KMS** | Tác vụ nền, dịch vụ, tiến trình, IP giả `10.0.0.10`, đường dẫn registry WoW64, registry Office KMS |
| 4 | **Dấu vết KMS38** | `GenuineTicket.xml`, `gatherosstate.exe`, `clipup.exe` |
| 5 | **Dấu thời gian TSforge** | Thời gian sửa đổi `data.dat` so với ngày cài Windows + tương quan với Event ID 19 (Windows Update) |
| 6 | **Nhật ký Sự kiện SPP** | Event ID 12288/12289/12290/8198 — gắn cờ địa chỉ máy chủ KMS ngoài trong dữ liệu event 12290 |
| 7 | **Thông báo pháp lý / giới hạn** | Hiển thị ở cuối lần quét; ghi lại các trường hợp dương tính giả đã biết |

> **Lưu ý:** Kích hoạt HWID (MAS) tạo Digital License thật qua API của Microsoft — **không thể phát hiện** bằng bất kỳ lệnh quét cục bộ nào. Điều này được ghi lại trong kết quả kiểm tra.

#### ⚙️ Tự động Cập Nhật Mặc Định Quét

- **Ứng dụng:** Hộp thoại Cài đặt → nút `↻ Cập nhật mặc định` — tải `settings.default.ini` từ GitHub
- **CLI:** Tùy chọn `U` trong menu chính
- Dự phòng tích hợp: 30 hậu tố GVLK và 14 tên miền KMS vi phạm khi không có mạng
- `settings.ini` chia thành `[Default]` (tự động quản lý) và `[User]` (không bao giờ bị ghi đè)

#### 🔑 Quản Lý Key Dự Phòng (Tùy chọn 3)

- Phát hiện sự không khớp giữa `BackupProductKeyDefault` (registry) và key hiện đang hoạt động
- **Admin:** Hộp thoại xác nhận để xóa key dự phòng cũ
- **Không phải Admin:** Hiển thị `⚠ Chạy với quyền Administrator để xóa key dự phòng cũ.` trong nhật ký
- Hệ thống DE (Digital Entitlement): sự không khớp được ghi chú đúng là bình thường/thông tin

#### ℹ️ Kiểm Tra Phiên Bản trong Hộp Thoại Giới Thiệu

- Mở ra với `Đang kiểm tra cập nhật…`
- Truy vấn `api.github.com/repos/ardennguyen/WinLic/releases/latest` với thời gian chờ 8 giây
- **Đã cập nhật:** `✔ Bạn đang dùng phiên bản mới nhất.` (màu xanh)
- **Có phiên bản mới:** `↑ Có phiên bản mới: vX.X` — hyperlink có thể nhấn để mở trang Releases

### Sửa Lỗi

| Lỗi | Mô tả |
|-----|-------|
| Thứ tự khôi phục nhật ký | Nhật ký được khôi phục sau nâng quyền UAC nay hiển thị **phía trên** thông báo "Sẵn sàng" |
| Nhãn phiên bản | `v1.0 (beta)` ở góc trái được mã hóa cứng — nay được lấy từ `Localization.cs`, luôn đồng bộ |
| Key dự phòng không có quyền Admin | Không có thông báo khi phát hiện không khớp mà không có quyền Admin — nay hiển thị cảnh báo `⚠` |
| Nút Cập nhật mặc định bị đè | Nhãn rút gọn thành `↻ Cập nhật mặc định`; chiều rộng giảm từ 160 → 110 px |
| Lớp 6 nhật ký sự kiện SPP (PS1) | Địa chỉ máy chủ ngoài được trích xuất đúng từ nội dung sự kiện 12290 |
| Registry KMS WoW64 (PS1) | Bổ sung `HKLM:\SOFTWARE\WOW6432Node\...\SoftwareProtectionPlatform` làm đường dẫn quét phụ |
| So sánh InstallDate (PS1) | `InstallDate` được đọc dưới dạng Unix epoch và so sánh với thời gian ghi của `data.dat` |
| Quét `clipup.exe` (PS1) | Đã thêm vào danh sách quét tiến trình để phát hiện KMS38 |

### Giới Hạn Đã Biết

- Không thể phát hiện kích hoạt **MAS HWID** — tạo Digital Entitlement thật qua API Microsoft
- Công cụ **đã được gỡ hoàn toàn sau khi dùng** không để lại dấu vết để phát hiện
- **Windows Loader** (vá SLIC/BIOS) không được phát hiện đáng tin cậy
- Máy chủ KMS doanh nghiệp hợp pháp có thể kích hoạt cảnh báo lớp GVLK — cần xét theo ngữ cảnh

### Nâng Cấp Từ v1.0-beta2

1. Thay `WinLicApp.exe` bằng bản mới — không cần cài đặt
2. Thay `WinLicManager.ps1` — định dạng `settings.ini` không đổi; phần `[User]` được giữ nguyên
3. Tùy chọn nhấn `↻ Cập nhật mặc định` (ứng dụng) hoặc nhấn `U` (CLI) để đồng bộ danh sách quét mới nhất

### Mã Kiểm Tra Toàn Vẹn

```
F9AA759E88C3320FB2F3F5BDDCB2B1189FDDCEA3D7199E2D2E40BC3971B9BA3C  WinLicApp-v1.3-beta1.exe
4DB25DBEB92281359E45527E81876ACC26B28C699CAA47B660922642335000B5  WinLicApp-v1.3-beta1.zip
77AED3AA07B4537DA75B6C270953391F6FEAE140E56DF1F8A5652721048FEF08  WinLicManager-v1.3-beta1.ps1
5E9A2CE9DA9A4DCFABE64F2DEFCDE892BE7EEEB6F60D5380B647323835454A65  WinLicPS-v1.3-beta1.zip
```

**Xác minh (PowerShell):**
```powershell
Get-FileHash .\WinLicApp-v1.3-beta1.zip    -Algorithm SHA256
Get-FileHash .\WinLicPS-v1.3-beta1.zip     -Algorithm SHA256
Get-FileHash .\WinLicManager-v1.3-beta1.ps1 -Algorithm SHA256
```
