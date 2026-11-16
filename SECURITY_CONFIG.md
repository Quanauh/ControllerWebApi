# Cấu hình bí mật

Chạy local: tạo file `.env` từ `.env.example` trong thư mục `HuanBank.Api`, điền
khóa JWT và chuỗi kết nối Oracle đã đổi mật khẩu, rồi chạy `dotnet run`. Ứng dụng
chỉ đọc `.env` khi `ASPNETCORE_ENVIRONMENT=Development`. File `.env` đã được Git
bỏ qua và không được đưa vào bản zip. Biến môi trường hệ điều hành có ưu tiên cao
hơn giá trị trong `.env`.

Ứng dụng đọc khóa ký JWT từ `Jwt__Key` và chuỗi kết nối Oracle từ
`ConnectionStrings__Oracle`. Hai giá trị này phải được cấp qua biến môi trường
hoặc kho bí mật của môi trường triển khai. Ứng dụng dừng khi thiếu một trong hai
giá trị; khóa JWT phải dài ít nhất 32 byte UTF-8.

Ví dụ cho phiên PowerShell hiện tại (thay thông tin Oracle bằng tài khoản đã
được đổi mật khẩu):

```powershell
$env:Jwt__Key = [Convert]::ToBase64String(
    [Security.Cryptography.RandomNumberGenerator]::GetBytes(64))
$env:ConnectionStrings__Oracle = 'User Id=HUANBANK;Password=<new-password>;Data Source=<host>:1521/<service>'
dotnet run --project .
```

Hãy lưu khóa JWT được tạo trong kho bí mật để các instance dùng cùng một khóa.
Đổi mật khẩu tài khoản Oracle tại máy chủ và cập nhật chuỗi kết nối tương ứng.
Đổi khóa JWT sẽ làm các token cũ hết hiệu lực. Vì bí mật cũ đã xuất hiện trong
mã nguồn và bản sao lưu, cần coi chúng là đã lộ và thu hồi tại môi trường thật.

Không đưa giá trị bí mật vào `appsettings*.json`, tài liệu, commit hoặc file zip.
