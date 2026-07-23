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

## Mức kết quả

- `CLEAN`: chưa có tín hiệu xấu.
- `SUSPICIOUS`: có dấu hiệu yếu hoặc chưa đủ evidence độc lập.
- `HIGH_RISK`: có ít nhất hai tín hiệu mạnh độc lập, hoặc một tín hiệu đang
  hoạt động mang tính xác định.
- `INCONCLUSIVE`: không đọc đủ một hoặc nhiều nguồn dữ liệu quan trọng.
- `SCAN_ERROR`: scanner chính không thể hoàn tất.

## Phạm vi chỉ đọc

Scanner chỉ dùng WMI `SELECT`, Registry read-only, Event Log read-only,
`slmgr.vbs /xpr`, `slmgr.vbs /dlv` và `schtasks /query /fo csv /v /nh`.
Scanner không kích hoạt, cài/gỡ key, rearm, xóa KMS, sửa task, dừng service,
xóa file hoặc sửa Registry.

`NoGenTicket` chỉ được đọc tại:

`HKLM\SOFTWARE\Policies\Microsoft\Windows NT\CurrentVersion\Software Protection Platform`

Microsoft tài liệu hóa giá trị này là policy **Turn off KMS Client Online AVS
Validation**, nên sự hiện diện của nó được báo trung lập và không phải bằng
chứng crack:

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
- Event Log hoặc Scheduled Tasks có thể trả `UNKNOWN` khi quyền hiện tại không
  đủ, nhưng scanner không tự yêu cầu Administrator.
