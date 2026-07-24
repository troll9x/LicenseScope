# Phân tích dấu vết kích hoạt Windows

Nút **Phân tích dấu vết crack** chạy bảy nhóm kiểm tra chỉ đọc:

1. KMS Crack
2. MAS / HWID
3. KMS38 Hook
4. Logic bản quyền
5. Thư mục công cụ
6. Tác vụ ẩn (Scheduled Tasks)
7. Can thiệp Registry

## Kết quả nhị phân

Analyzer không suy đoán hợp pháp hay trái phép. Nó ghi nhận bốn sự kiện khách
quan dưới dạng `true/false` hoặc `CÓ/KHÔNG`:

- `ScanCompleted`: các nguồn bắt buộc của chế độ quét đã đọc xong hay chưa.
- `ActivationDetected`: Windows có báo trạng thái kích hoạt hay không.
- `TraceDetected`: có ít nhất một bằng chứng khớp allowlist hay không.
- `ProvenanceVerified`: có bằng chứng xác minh nguồn gốc license hay không.

`TraceDetected: CÓ` luôn đi kèm `Evidence` có cấu trúc:

`<scanner-id> | <nguồn>: <tên> [<đường dẫn>] -> <hành động>`

Ví dụ:

`scheduled-tasks | ScheduledTask: AutoKMS -> C:\Tools\AutoKMS.exe`

`TraceDetected: KHÔNG` có nghĩa chính xác là scanner không quan sát được bằng
chứng khớp allowlist. Nếu một nguồn bắt buộc bị từ chối hoặc lỗi,
`ScanCompleted` đồng thời là `KHÔNG`; lỗi không được biến thành tuyên bố máy
không có dấu vết.

Các nhãn suy diễn `SUSPICIOUS`, `HIGH_RISK`, `INCONCLUSIVE` và `SCAN_ERROR`
không được xuất ra GUI, CLI, JSON, CSV hoặc HTML.

## Phạm vi chỉ đọc

Scanner thường chỉ dùng WMI `SELECT`, Registry read-only,
`slmgr.vbs /xpr`, `slmgr.vbs /dlv` và `schtasks /query /fo csv /v /nh`.
Scanner không kích hoạt, cài/gỡ key, rearm, xóa KMS, sửa task, dừng service,
xóa file hoặc sửa Registry.

## Deep forensic scan

Deep forensic scan tắt mặc định và chỉ chạy sau khi người dùng đồng ý rõ ràng.
Chế độ này chỉ đọc:

- event log cấp phép Windows có liên quan;
- PowerShell Operational khi logging tồn tại;
- lịch sử phát hiện Windows Defender có liên quan;
- tên Prefetch khớp allowlist;
- Amcache khi có thể truy vấn entry allowlist mà không duyệt dữ liệu không liên
  quan.

Không quét file người dùng, không upload dữ liệu và không xóa hoặc sửa dữ liệu.
Nguồn không tồn tại hoặc bị từ chối được ghi bằng `Checked: false`; nếu nguồn đó
được yêu cầu cho lần quét, `ScanCompleted` là `false`.

## NoGenTicket

`NoGenTicket` chỉ được đọc tại:

`HKLM\SOFTWARE\Policies\Microsoft\Windows NT\CurrentVersion\Software Protection Platform`

Microsoft tài liệu hóa giá trị này là policy **Turn off KMS Client Online AVS
Validation**. Nếu giá trị tồn tại, analyzer báo khách quan
`TraceDetected: true` và đưa đúng registry path/value vào `Evidence`; nó không
đổi sự kiện đó thành tuyên bố pháp lý về license.

<https://learn.microsoft.com/windows/privacy/manage-connections-from-windows-operating-system-components-to-microsoft-services#19-software-protection-platform>

KMS là cơ chế kích hoạt hợp lệ cho tổ chức và client KMS dùng chu kỳ gia hạn
180 ngày:

<https://learn.microsoft.com/windows-server/get-started/activation-troubleshoot-kms-general>

Các property WMI bản quyền sử dụng bởi scanner được Microsoft mô tả tại:

<https://learn.microsoft.com/previous-versions/windows/desktop/sppwmi/softwarelicensingproduct>

## Giới hạn quan sát

- Công cụ đã gỡ sạch có thể không còn bằng chứng khớp allowlist.
- Allowlist không bao phủ mọi công cụ hoặc phiên bản trong tương lai.
- Trạng thái kích hoạt hiện tại không tự xác minh nguồn gốc digital entitlement.
- Các giới hạn trên được thể hiện bằng trường nhị phân và coverage cụ thể,
  không bằng verdict suy diễn.
