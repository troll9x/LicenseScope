# WinLic Manager — v1.0 (beta1)

> Phát hành: 2026-06-29 · Nền tảng: Windows 10/11 · Framework: .NET Framework 4.8 (tích hợp sẵn)

---

## 📥 Tải về / Download

| File | Mô tả |
|---|---|
| `WinLicApp-v1.0-beta1.zip` | **File chính** — giải nén và chạy, không cần cài đặt |
| `WinLicApp-v1.0-beta1.sha256.txt` | File kiểm tra toàn vẹn (SHA-256 + MD5) |

### Xác minh toàn vẹn / Verify integrity

```powershell
# PowerShell
Get-FileHash .\WinLicApp-v1.0-beta1.zip -Algorithm SHA256
```
```cmd
:: CMD / certutil
certutil -hashfile WinLicApp-v1.0-beta1.zip SHA256
```

So sánh kết quả với nội dung file `WinLicApp-v1.0-beta1.sha256.txt`.

---

## 🆕 Thay đổi trong beta1 / What's new in beta1

### Giao diện / UI

- **Thanh bên tự co giãn** — chiều rộng sidebar tự động mở rộng vừa với nhãn dài nhất ở bất kỳ ngôn ngữ nào; nhãn tiếng Việt ở Tùy chọn 7 không còn bị cắt.
- **Thống nhất định dạng nhãn** — tất cả 7 nút tùy chọn đều dùng dấu `—` (ví dụ `1 — Phiên bản OS & Key OEM BIOS`), đồng nhất với nút 6 và 7 ở phiên bản trước.
- **Log tự xuống dòng** — nội dung nhật ký dài không còn yêu cầu cuộn ngang; văn bản tự ngắt dòng theo kích thước cửa sổ.
- **Nhãn nút rõ ràng hơn** — tất cả 7 tùy chọn được đổi tên mô tả chức năng cụ thể hơn (cả EN và VIE).

### Hộp thoại / Dialogs

- **slmgr /dlv** — hộp thoại xác nhận nay liệt kê đầy đủ những gì báo cáo mở rộng tiết lộ: kênh bản quyền, SKU, cấu hình KMS, số lần rearm còn lại, CMID, thời hạn kích hoạt.
- **Key Dự phòng Registry** — hộp thoại nay ghi rõ đường dẫn registry (`BackupProductKeyDefault`) và phân biệt rõ "key dự phòng" với "kích hoạt đang hoạt động" bằng cả hai ngôn ngữ.
- **Tiêu đề kết quả Option 7** — đổi từ chuỗi kỹ thuật (`P7_SummaryHeader`) sang `Kết quả` / `Results`.

### Tính năng mới / New features

- **⚙ Cài đặt Kiểm tra (Tùy chọn 7)** — nút mới ngay dưới Tùy chọn 7 mở hộp thoại cấu hình danh sách quét:
  - Xem danh sách **mặc định tích hợp sẵn** (chỉ đọc) cho từng loại: cổng KMS, tên dịch vụ, từ khóa tác vụ, tên tiến trình
  - Thêm **mục tùy chỉnh** cho mỗi loại (một dòng một mục), bao gồm cả đường dẫn tập tin/thư mục riêng
  - Tự động lưu vào `settings.ini` cạnh file EXE; tự tải lại khi khởi động

- **Quét nhiều cổng KMS** — Tùy chọn 7 nay kiểm tra tất cả cổng KMS được cấu hình (mặc định: 1688; có thể thêm cổng tùy chỉnh qua Settings).

### Kỹ thuật / Technical

- Tách `AppSettings.cs` — module độc lập quản lý đọc/ghi `settings.ini`
- Tách `SettingsDialog.xaml` + `SettingsDialog.xaml.cs` — hộp thoại cài đặt

---

## 🔧 Yêu cầu hệ thống / System requirements

| | |
|---|---|
| **OS** | Windows 10 (1903+) hoặc Windows 11 |
| **Kiến trúc** | x64 |
| **.NET Framework** | 4.8 — **đã tích hợp sẵn** trong Windows 10/11 |
| **Quyền** | Administrator (ứng dụng tự yêu cầu UAC) |

---

## ⚠️ Lưu ý / Notes

- Đây là bản **beta** — có thể còn lỗi. Vui lòng báo cáo qua [Issues](https://github.com/ardennguyen/WinLic/issues).
- Các tùy chọn **4, 5, 6** thay đổi trạng thái bản quyền thật sự — chỉ dùng khi bạn hiểu rõ tác động.
- File `settings.ini` được tạo tự động cạnh EXE khi bạn lưu cài đặt; xóa file này để khôi phục mặc định.

---

## 📋 Changelog đầy đủ / Full changelog

Xem commit [`0e61019`](https://github.com/ardennguyen/WinLic/commit/0e61019) để biết chi tiết kỹ thuật.

---

*WinLic Manager v1.0 (beta1) — Made with ❤️ by [Arden Nguyen Duc Huy](https://github.com/ardennguyen)*
