# WinLic command-line guide

`audit --all` runs Adobe fourth after Autodesk. WinLic neither downloads the Adobe toolkit nor accepts an arbitrary toolkit path; a future installer/admin may provision it under the application-managed trusted tools directory.

`audit --all` includes Autodesk read-only. Exit code 2 may mean entitlement cannot be verified offline; WinLic does not contact Autodesk Account or a license server.

Mở Command Prompt tại thư mục chứa chương trình và chạy:

```text
WinLicAudit.Cli.exe audit --all
```

Xuất ba báo cáo:

```text
WinLicAudit.Cli.exe audit --all --format json,csv,html --output ".\reports"
```

Dùng `WinLicAudit.Cli.exe --help` để xem tùy chọn. Exit code 0 là hoàn tất sạch; 1 có Unlicensed/Expired; 2 có dữ liệu chưa đủ; 3 lỗi hệ thống; 4 tham số sai; 5 ghi report lỗi; 6 bị hủy. CLI chỉ đọc và không yêu cầu PowerShell script.
