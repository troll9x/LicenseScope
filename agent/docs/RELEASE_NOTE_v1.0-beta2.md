# WinLic Manager — Release Notes / Ghi Chú Phát Hành
## v1.0-beta2

**Branch:** [`release/v1.0-beta2`](https://github.com/ardennguyen/WinLic/tree/release/v1.0-beta2)

---

🇺🇸 [English](#english) · 🇻🇳 [Tiếng Việt](#tiếng-việt)

---

<a id="english"></a>
## 🇺🇸 English

**Release date:** 2026-06-29
**Platforms:** Windows 10 (1903+) / Windows 11 x64

### What's included

| File | Description |
|---|---|
| `WinLicApp-v1.0-beta2.zip` | GUI application — extract and run, no install needed |
| `WinLicApp-v1.0-beta2.exe.sha256` | SHA-256 hash of the EXE |
| `WinLicApp-v1.0-beta2.zip.sha256` | SHA-256 hash of the ZIP |
| `WinLicPS-v1.0-beta2.zip` | PowerShell CLI tool (`WinLicManager.ps1` + `settings.ini`) |
| `WinLicPS-v1.0-beta2.zip.sha256` | SHA-256 hash of the PS ZIP |

### New in this release

#### 🆕 PowerShell CLI — `WinLicManager.ps1`

A fully-featured command-line counterpart to the GUI, shipped as a separate
`WinLicPS-v1.0-beta2.zip` package. Mirrors all 7 options of the GUI application.

- Identical 7-option menu with the same logic as the GUI
- Deferred elevation — runs without Admin; options 4, 5, 6 prompt for relaunch only when selected
- Options 4, 5, 6 highlighted in red `[!]` at all times as a danger indicator
- About section displayed on first launch only (not repeated on menu returns)
- **Option 4** — confirmation screen before key installation: shows current edition,
  partial key, and activation status before applying any change
- **Option 7** — 3rd Party Activation Audit with:
  - Live scan configuration summary at startup (ports, services, tasks, processes, file paths)
  - Option to open and edit `settings.ini` in Notepad before scanning; reloads config after close
  - Full 6-layer scan identical to the GUI audit
- `settings.ini` support — extend scan lists without editing the script

#### 🔧 GUI App — Bug fixes & improvements

**Log persistence across elevation relaunch**

When a standard user selects option 4, 5, or 6 and confirms the Admin relaunch,
the current log is now automatically saved to `%TEMP%\winlic_session.log` before
the app closes. The elevated instance restores the log on startup, displayed in
a muted color with a clear `▲ Log restored from previous session` header.

> `%TEMP%` resolves to the current user's private temp folder
> (`C:\Users\<user>\AppData\Local\Temp`) and is always writable by any user
> account, including standard (non-admin) accounts.

**Vietnamese translation corrections**

All occurrences of the incomplete phrase "Khởi lại" have been corrected:

| Location | Before | After |
|---|---|---|
| Relaunch button | `Khởi Lại Với Quyền Admin` | `Khởi Chạy Lại Với Quyền Admin` |
| Elevation dialog | `Khởi lại với quyền…` | `Khởi chạy lại với quyền…` |
| Admin required message | `…nút '⚡ Khởi Lại…'` | `…nút '⚡ Khởi Chạy Lại…'` |
| From-option dialog | `Khởi lại WinLic Manager…` | `Khởi chạy lại WinLic Manager…` |
| Elevation failed | `Không thể nâng cấp quyền` | `Không thể yêu cầu nâng quyền` |

**Elevation dialog note updated**

> Before: *"The app will restart and the current log will be cleared."*
>
> After: *"The previous session log will be preserved."*

**Version strings**

All in-app version strings updated to `v1.0 (beta2)` — About dialog, status bar, startup message.

### Known limitations

- **MAS HWID / HWIDGEN** activations cannot be detected — they create a genuine
  Digital Entitlement via Microsoft's own API, indistinguishable from a purchased license
- If an activation tool was **fully removed after use**, no traces remain and detection is impossible
- **Windows Loader** (SLIC/BIOS patching) and fully cleaned **KMS38** are not reliably detected

### Upgrading from v1.0-beta1

No migration steps required. Replace the old EXE with the new one.
The `settings.ini` format is unchanged — existing customizations carry over.

### Checksums

```
2e279c0a27cd6bd103f8974ff9e953352697542e074daeebc0ba24925afb07c7  WinLicApp-v1.0-beta2.exe
5d168e1f7299d0eff08c402dbe289e55e49245f9f64840e4fe9c923ab1b2491c  WinLicApp-v1.0-beta2.zip
6924a471be756e75763fc5d97e0604a145ddc8b1e553120d9718df3ec248b6af  WinLicPS-v1.0-beta2.zip
```

**Verify (PowerShell):**
```powershell
Get-FileHash .\WinLicApp-v1.0-beta2.zip -Algorithm SHA256
Get-FileHash .\WinLicPS-v1.0-beta2.zip  -Algorithm SHA256
```

---

<a id="tiếng-việt"></a>
## 🇻🇳 Tiếng Việt

**Ngày phát hành:** 2026-06-29
**Nền tảng:** Windows 10 (1903+) / Windows 11 x64

### Nội dung bản phát hành

| File | Mô tả |
|---|---|
| `WinLicApp-v1.0-beta2.zip` | Ứng dụng GUI — giải nén và chạy, không cần cài đặt |
| `WinLicApp-v1.0-beta2.exe.sha256` | Mã băm SHA-256 của file EXE |
| `WinLicApp-v1.0-beta2.zip.sha256` | Mã băm SHA-256 của file ZIP |
| `WinLicPS-v1.0-beta2.zip` | Công cụ PowerShell CLI (`WinLicManager.ps1` + `settings.ini`) |
| `WinLicPS-v1.0-beta2.zip.sha256` | Mã băm SHA-256 của bộ cài CLI |

### Điểm mới trong phiên bản này

#### 🆕 PowerShell CLI — `WinLicManager.ps1`

Công cụ dòng lệnh tương đương với ứng dụng GUI, được đóng gói riêng trong
`WinLicPS-v1.0-beta2.zip`. Hỗ trợ đầy đủ 7 tùy chọn giống ứng dụng GUI.

- Menu 7 tùy chọn giống hệt GUI, cùng logic xử lý
- Nâng quyền trì hoãn — chạy không cần Admin; tùy chọn 4, 5, 6 chỉ nhắc khi được chọn
- Tùy chọn 4, 5, 6 luôn hiển thị màu đỏ `[!]` để cảnh báo nguy hiểm
- Phần Giới thiệu chỉ hiển thị lần đầu khởi động (không lặp lại mỗi lần quay về menu)
- **Tùy chọn 4** — màn hình xác nhận trước khi cài key: hiển thị ấn bản hiện tại,
  key một phần và trạng thái kích hoạt trước khi thực hiện thay đổi
- **Tùy chọn 7** — Kiểm Tra Kích Hoạt Bên Thứ Ba với:
  - Tóm tắt cấu hình quét khi khởi động (cổng, dịch vụ, tác vụ, tiến trình, đường dẫn)
  - Tùy chọn mở và chỉnh sửa `settings.ini` trong Notepad trước khi quét; tải lại sau khi đóng
  - Quét 6 lớp đầy đủ giống hệt kiểm tra trong GUI
- Hỗ trợ `settings.ini` — mở rộng danh sách quét mà không cần chỉnh sửa script

#### 🔧 Ứng dụng GUI — Sửa lỗi & cải tiến

**Lưu nhật ký qua các lần nâng quyền**

Khi người dùng thường chọn tùy chọn 4, 5, hoặc 6 và xác nhận khởi chạy lại với quyền Admin,
nhật ký hiện tại sẽ tự động được lưu vào `%TEMP%\winlic_session.log` trước khi ứng dụng đóng.
Phiên làm việc được nâng quyền sẽ khôi phục nhật ký khi khởi động, hiển thị bằng màu xám nhạt
với tiêu đề rõ ràng `▲ Nhật ký được khôi phục từ phiên trước`.

> `%TEMP%` trỏ đến thư mục tạm riêng của người dùng hiện tại
> (`C:\Users\<tên người dùng>\AppData\Local\Temp`) và luôn có thể ghi,
> kể cả với tài khoản thường (không có quyền Admin).

**Sửa lỗi dịch thuật tiếng Việt**

Tất cả các trường hợp dùng sai cụm từ "Khởi lại" đã được sửa:

| Vị trí | Trước | Sau |
|---|---|---|
| Nút nâng quyền | `Khởi Lại Với Quyền Admin` | `Khởi Chạy Lại Với Quyền Admin` |
| Hộp thoại nâng quyền | `Khởi lại với quyền…` | `Khởi chạy lại với quyền…` |
| Thông báo cần Admin | `…nút '⚡ Khởi Lại…'` | `…nút '⚡ Khởi Chạy Lại…'` |
| Hộp thoại từ tùy chọn | `Khởi lại WinLic Manager…` | `Khởi chạy lại WinLic Manager…` |
| Lỗi nâng quyền | `Không thể nâng cấp quyền` | `Không thể yêu cầu nâng quyền` |

**Cập nhật thông báo trong hộp thoại nâng quyền**

> Trước: *"Ứng dụng sẽ khởi động lại và nhật ký hiện tại sẽ bị xóa."*
>
> Sau: *"Nhật ký phiên trước sẽ được giữ lại."*

**Chuỗi phiên bản**

Tất cả chuỗi phiên bản trong ứng dụng đã được cập nhật thành `v1.0 (beta2)` — hộp thoại Giới thiệu, thanh trạng thái, thông báo khởi động.

### Giới hạn đã biết

- Không thể phát hiện kích hoạt bằng **MAS HWID / HWIDGEN** — các công cụ này tạo
  Digital Entitlement thật qua API của Microsoft, không thể phân biệt với bản quyền mua
- Nếu công cụ kích hoạt đã **được gỡ hoàn toàn sau khi sử dụng**, không còn dấu vết để phát hiện
- **Windows Loader** (vá SLIC/BIOS) và **KMS38** đã dọn sạch không được phát hiện đáng tin cậy

### Nâng cấp từ v1.0-beta1

Không cần thực hiện bước chuyển đổi nào. Thay thế file EXE cũ bằng file mới.
Định dạng `settings.ini` không thay đổi — các tùy chỉnh hiện có vẫn hoạt động.

### Mã kiểm tra toàn vẹn

```
2e279c0a27cd6bd103f8974ff9e953352697542e074daeebc0ba24925afb07c7  WinLicApp-v1.0-beta2.exe
5d168e1f7299d0eff08c402dbe289e55e49245f9f64840e4fe9c923ab1b2491c  WinLicApp-v1.0-beta2.zip
6924a471be756e75763fc5d97e0604a145ddc8b1e553120d9718df3ec248b6af  WinLicPS-v1.0-beta2.zip
```

**Xác minh (PowerShell):**
```powershell
Get-FileHash .\WinLicApp-v1.0-beta2.zip -Algorithm SHA256
Get-FileHash .\WinLicPS-v1.0-beta2.zip  -Algorithm SHA256
```
