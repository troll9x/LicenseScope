# Báo cáo WinLic

Autodesk rows contain sanitized product, registration, method, and service evidence. Helper stdout, serials, accounts, and license-server addresses are not retained.

- JSON phù hợp lưu trữ và tích hợp hệ thống.
- CSV dùng UTF-8 để mở bằng Excel; dữ liệu được bảo vệ khỏi công thức CSV injection.
- HTML là file độc lập, không cần Internet và có thể in thành PDF bằng trình duyệt.

Mặc định báo cáo gồm evidence và warning nhưng bỏ tên máy. Product key chỉ còn năm ký tự cuối, email/GUID/path người dùng được che, và trường token/password/cookie/credential bị loại. File hiện có không bị ghi đè trừ khi người dùng hoặc CLI bật `--overwrite`.
