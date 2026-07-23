# Dữ liệu quét giả lập

`license-audit-simulation.json` chứa dữ liệu thử nghiệm cho Adobe, Autodesk và
SketchUp. Trong ứng dụng, chọn **Quét file giả lập** rồi mở tệp này.

Tệp giả lập không ghi Registry, không gọi công cụ cấp phép của nhà cung cấp và
không phản ánh trạng thái thật của máy. Các hàng được nạp từ tệp có mã nội bộ
`SIMULATION`, hiển thị cảnh báo trong chi tiết và không cho phép gỡ phần mềm.

Các giá trị `status` hợp lệ là tên trong `LicenseStatus`, ví dụ:
`Licensed`, `Unlicensed`, `Trial`, `GracePeriod`, `Expired`,
`NeedsSignIn`, `NeedsOnlineVerification`, `Unknown`, `Unsupported`, `Error`.

Chỉ ba `scannerId` sau được chấp nhận:

- `adobe.desktop`
- `autodesk.desktop`
- `trimble.sketchup`
