# Phân tích dấu vết crack Windows

Nút **Phân tích dấu vết crack** chạy bảy nhóm kiểm tra chỉ đọc:

1. KMS Crack
2. MAS / HWID
3. KMS38 Hook
4. Logic bản quyền
5. Thư mục công cụ
6. Tác vụ ẩn (Scheduled Tasks)
7. Can thiệp Registry

Kết quả là đánh giá kỹ thuật, không phải kết luận pháp lý. Một dấu vết yếu,
một ngày hết hạn xa, Digital Entitlement, KMS doanh nghiệp hoặc một giá trị
Registry đơn lẻ không đủ để kết luận hệ thống dùng công cụ kích hoạt trái phép.

## Trạng thái và verdict

Trạng thái kích hoạt, dấu vết và nguồn gốc license là ba khái niệm riêng:

- `ACTIVATED`: chỉ xác nhận Windows hiện báo đang kích hoạt.
- `TRACE_NOT_FOUND`: chưa tìm thấy dấu vết có thể kiểm chứng trong phạm vi
  nguồn đã kiểm tra; không có nghĩa là “an toàn”, “bản quyền hợp lệ” hoặc
  “không sử dụng crack”.
- `SUSPICIOUS`: có dấu hiệu yếu hoặc chưa đủ evidence độc lập.
- `HIGH_RISK`: có ít nhất hai tín hiệu mạnh độc lập.
- `INCONCLUSIVE`: không đọc đủ nguồn dữ liệu quan trọng hoặc Windows đang được
  kích hoạt nhưng provenance không thể xác minh từ trạng thái hiện tại.
- `SCAN_ERROR`: scanner chính không thể hoàn tất.

`CONSISTENT_STATE` chỉ cho biết edition/channel quan sát được phù hợp với
firmware OEM; nó không phải `VERIFIED_PROVENANCE`.

Khi không có artifact, thông báo kết luận là:

> KHÔNG PHÁT HIỆN DẤU VẾT: Trong phạm vi các phép kiểm tra hiện tại, chưa tìm
> thấy dấu vết có thể kiểm chứng. Kết quả này không xác nhận nguồn gốc license
> hoặc chứng minh hệ thống chưa từng sử dụng công cụ kích hoạt.

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
- Amcache chỉ khi có thể truy vấn entry allowlist mà không duyệt inventory
  không liên quan.

Không quét file người dùng, không upload dữ liệu và không xóa hoặc sửa bất kỳ
thứ gì. Nguồn không có, bị từ chối hoặc không thể truy vấn trong privacy
boundary được báo `UNKNOWN`.

`NoGenTicket` chỉ được đọc tại:

`HKLM\SOFTWARE\Policies\Microsoft\Windows NT\CurrentVersion\Software Protection Platform`

Microsoft tài liệu hóa giá trị này là policy **Turn off KMS Client Online AVS
Validation**. Đây chỉ là evidence không mang tính kết luận; riêng giá trị này
không thể tạo `HIGH_RISK`:

<https://learn.microsoft.com/windows/privacy/manage-connections-from-windows-operating-system-components-to-microsoft-services#19-software-protection-platform>

KMS là cơ chế kích hoạt hợp lệ cho tổ chức và client KMS dùng chu kỳ gia hạn
180 ngày:

<https://learn.microsoft.com/windows-server/get-started/activation-troubleshoot-kms-general>

Các property WMI bản quyền sử dụng bởi scanner được Microsoft mô tả tại:

<https://learn.microsoft.com/previous-versions/windows/desktop/sppwmi/softwarelicensingproduct>

## Giới hạn

- Digital license hợp lệ và digital entitlement được tạo qua công cụ bên thứ
  ba có thể không phân biệt được chỉ từ evidence cục bộ.
- Công cụ đã gỡ sạch có thể không còn dấu vết.
- Danh sách đường dẫn/từ khóa là allowlist giới hạn; scanner không quét toàn ổ.
- Event Log, Amcache hoặc Scheduled Tasks có thể trả `UNKNOWN` khi quyền hiện tại không
  đủ, nhưng scanner không tự yêu cầu Administrator.
