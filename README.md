# License Scope

License Scope là công cụ kiểm tra tình trạng bản quyền phần mềm trên Windows. Dự án
cung cấp giao diện WPF và chương trình dòng lệnh, hoạt động chủ yếu ở chế độ chỉ
đọc và có thể xuất báo cáo JSON, CSV hoặc HTML đã che dữ liệu nhạy cảm.

## Khả năng tương thích

Một bộ cài `LicenseScope-Setup.exe` chứa cả hai bản chương trình và tự chọn đúng
kiến trúc khi cài:

| Windows / CPU | Payload được dùng | Trạng thái |
|---|---:|---|
| Windows 32-bit trên Intel hoặc AMD | x86 | Có |
| Windows 64-bit trên Intel hoặc AMD | x64 | Có |
| Windows 64-bit chạy tiến trình 32-bit | x86 qua WOW64 | Có |
| Windows 10/11 on Arm | x86 qua giả lập | Thử nghiệm, không phải ARM64 native |

`x32` thường được dùng để chỉ Windows 32-bit; tên kiến trúc chính xác trong dự án
là `x86`. Intel và AMD cùng dùng tập lệnh x86/x64 nên không cần hai bộ cài riêng
theo hãng CPU.

Yêu cầu hệ thống:

- .NET Framework 4.8. Bộ cài đã kèm bộ cài offline chính thức của Microsoft và
  chỉ chạy nó khi máy chưa có phiên bản phù hợp.
- Windows 7 phải có Service Pack 1; Windows 8.0 bị chặn; Windows 7 SP1 và 8.1 chỉ
  là đích tương thích kỹ thuật vì đã hết hỗ trợ bảo mật.
- Ma trận build tạo và kiểm tra PE x86/x64. Kiểm thử runtime hiện có được thực
  hiện trên Windows 10 22H2 x64, bao gồm cả tiến trình x64 và x86/WOW64. Không nên
  hiểu kết quả này là đã kiểm thử vật lý trên mọi thế hệ CPU hoặc mọi bản Windows.

Chạy lệnh sau sau khi cài để xem kiến trúc, .NET Framework và mức tương thích của
chính máy đang dùng:

```powershell
LicenseScope.Cli.exe compatibility
LicenseScope.Cli.exe compatibility --json
```

## Nội dung kiểm tra

License Scope hiện kiểm tra:

- Windows (`microsoft.windows`)
- Microsoft Office, Project và Visio (`microsoft.office`)
- Autodesk desktop (`autodesk.desktop`)
- Adobe desktop (`adobe.desktop`)
- Trimble SketchUp (`trimble.sketchup`)
- Dấu vết liên quan đến crack/kích hoạt không chuẩn trên Windows bằng bộ phân
  tích chỉ đọc

Kết quả offline có thể là `Licensed`, `Unlicensed`, `Expired`,
`NeedsOnlineVerification`, `Unknown` hoặc `Error`. Một sản phẩm cần xác minh
online không được tự động coi là không có bản quyền.

Bộ phân tích dấu vết hiển thị bốn kết quả nhị phân: **Quét hoàn tất**,
**Phát hiện kích hoạt**, **Phát hiện dấu vết** và
**Xác minh nguồn gốc giấy phép**. Báo cáo JSON vẫn giữ các khóa kỹ thuật
`scanCompleted`, `activationDetected`, `traceDetected` và
`provenanceVerified` để bảo đảm tương thích. Mọi kết quả `CÓ` đều kèm mã bộ
quét, nguồn và giá trị bằng chứng cụ thể.

## Cài đặt và sử dụng

1. Kiểm tra SHA-256 của bộ cài.
2. Chạy `LicenseScope-Setup.exe` và chấp nhận yêu cầu UAC.
3. Chọn English hoặc Vietnamese. Bộ cài tự chọn x86/x64.
4. Mở **License Scope** và chọn **Quét tất cả**.
5. Có thể xuất báo cáo JSON, CSV hoặc HTML từ giao diện.

Bản đóng gói nội bộ hiện chưa được ký Authenticode. Windows có thể hiện cảnh báo
SmartScreen; chỉ chạy tệp có SHA-256 trùng với manifest đi kèm.

Ví dụ dòng lệnh:

```powershell
LicenseScope.Cli.exe --version
LicenseScope.Cli.exe audit --all
LicenseScope.Cli.exe audit --all --format json,csv,html --output .\reports
LicenseScope.Cli.exe audit --all --no-evidence --quiet
LicenseScope.Cli.exe audit --all --deep-forensic-scan --consent-forensic-read
```

Quét pháp chứng chuyên sâu tắt mặc định. Chế độ này chỉ đọc các nguồn lịch sử
Windows nằm trong danh sách cho phép sau khi người dùng đồng ý rõ ràng; không
quét tệp người dùng, không tải dữ liệu lên mạng và không sửa hoặc xóa dữ liệu.

Mã thoát của lệnh audit:

| Mã | Ý nghĩa |
|---:|---|
| 0 | Hoàn tất, không có kết quả cần cảnh báo |
| 1 | Có sản phẩm `Unlicensed` hoặc `Expired` |
| 2 | Có dữ liệu chưa đủ để kết luận |
| 3 | Lỗi hệ thống |
| 4 | Tham số không hợp lệ |
| 5 | Không thể ghi báo cáo |
| 6 | Người dùng hủy |

## Quy tắc an toàn và riêng tư

Luồng audit là offline và chỉ đọc. Nó không cài hoặc xóa khóa sản phẩm, không kích
hoạt/rearm Windows, không tải báo cáo lên mạng và không gửi telemetry. Tên máy bị
loại khỏi báo cáo theo mặc định; khóa và bằng chứng nhạy cảm được che trước khi
ghi báo cáo.

Giao diện có hai thao tác khắc phục riêng, chỉ chạy sau khi người dùng xác nhận:

- **Xóa cấu hình KMS** chỉ gọi `slmgr.vbs /ckms` với quyền quản trị.
- **Gỡ phần mềm** chỉ mở trình gỡ đã đăng ký với Windows cho phần mềm đã cài nhưng
  chưa được xác nhận là có bản quyền. Windows không bao giờ được đưa vào danh sách
  gỡ.

## Build và kiểm thử

Yêu cầu để build:

- Windows
- .NET SDK có thể build dự án .NET Framework 4.8
- .NET Framework 4.8 Developer/Targeting Pack
- Inno Setup 7 để tạo bộ cài
- Tệp prerequisite đã xác minh theo
  `installer\prerequisites\dotnet48-runtime.json`

Build và chạy toàn bộ test:

```powershell
dotnet restore LicenseScope.sln --locked-mode
dotnet build LicenseScope.sln -c Release --no-restore
dotnet test LicenseScope.sln -c Release --no-build
```

Chạy đầy đủ hai cấu hình và ma trận kiến trúc từ PowerShell:

```powershell
.\build\Invoke-CI.ps1 -Configuration All -Platforms @('AnyCPU','x86','x64')
```

Tạo và kiểm tra bộ cài universal:

```powershell
.\build\Build-Installer.ps1
.\build\Test-Installer.ps1
```

Kết quả nằm tại:

- `artifacts\installer\LicenseScope-Setup.exe`
- `artifacts\installer\build-manifest.json`
- `artifacts\staging\x86` và `artifacts\staging\x64`

Các script build xác minh chữ ký và SHA-256 của bộ cài .NET Framework offline,
kiến trúc PE của GUI/CLI, allowlist payload và nội dung cấm trước khi tạo Setup.

## Cấu trúc tài liệu

- Hướng dẫn người dùng: `docs\user-guide`
- Build bộ cài: `docs\installer`
- Quy trình phát hành và xác minh: `docs\release`
- Bảo mật và chuỗi cung ứng: `docs\security`
- Nguồn gốc mã: `UPSTREAM.md`, `NOTICE.md`, `PROVENANCE.md`

## Giới hạn phân phối

Dự án tiếp nối mã WinLic với quyền phát triển riêng do người dùng quản lý. Lịch
sử repository hiện có không chứa nội dung giấy phép mà README upstream cũ từng
tham chiếu, vì vậy repository này không tự tuyên bố mã kế thừa là MIT và không tự
tạo giấy phép mới cho phần mã đó.

Việc phân phối công khai mã nguồn hoặc binary nằm ngoài phạm vi hiện tại. Xem
`docs\audit\phase-11-upstream-license-audit.md` để biết chi tiết.
