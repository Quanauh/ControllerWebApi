# Báo cáo thay đổi HuanBank.Api

Ngày: 09/11/2026

## 1. Cấu hình bí mật

- Xóa khóa ký JWT khỏi `appsettings.json` và xóa chuỗi kết nối Oracle cố định khỏi `util/Connectdb.cs`.
- Ứng dụng đọc khóa JWT từ `Jwt__Key` và chuỗi kết nối Oracle từ `ConnectionStrings__Oracle`. Khi thiếu cấu hình hoặc khóa JWT ngắn hơn 32 byte UTF-8, ứng dụng dừng lúc khởi động với thông báo lỗi.
- Chuyển `Database` sang nhận cấu hình qua dependency injection; cập nhật `AccountDAO` và `FakeAccountDAO` để dùng đối tượng này.
- Trong môi trường `Development`, ứng dụng đọc file `.env` ở thư mục project. Biến môi trường hệ điều hành được ưu tiên nếu cùng tên. Đã tạo `.env` local với khóa JWT mới; giá trị `ConnectionStrings__Oracle` đang để trống, chờ mật khẩu Oracle mới.
- Thêm `.gitignore` để bỏ qua `.env`, `.env.example` làm mẫu và `SECURITY_CONFIG.md` hướng dẫn cấu hình.
- Tạo lại `HuanBank.Api.zip` từ mã nguồn hiện tại, không gồm `.env`, Git, log và thư mục build. Đã kiểm tra bản zip không chứa khóa JWT hoặc mật khẩu Oracle cũ.

## 2. Kết quả giao dịch

- Trước đây `Deposit`, `Withdraw` và `TransferMoney` trả về enum `TransactionResult`.
- Theo mẫu trong `settling_service.zip`, ba phương thức hiện trả về `BusinessResponse` gồm `Code` và `Message`; thêm `TransactionCodes` để đặt tên cho các mã kết quả.
- Cập nhật `IBankService`, `BankService` và `TransactionController`; bỏ enum `TransactionResult`. Controller vẫn dùng HTTP 200 cho thành công, 400 cho dữ liệu hoặc số dư không hợp lệ, và 404 khi không tìm thấy tài khoản. Phản hồi giao dịch nay có thêm `code`.
- `LoginResult` và `RegistrationResult` không thuộc phạm vi thay đổi này.

## 3. Kiểm tra và việc còn lại

- `dotnet build --no-restore`: thành công, 0 lỗi và 0 cảnh báo.
- Đã xác nhận ứng dụng dừng khi thiếu khóa JWT và khi chuỗi kết nối Oracle trong `.env` còn trống. Chưa kiểm thử giao dịch với Oracle.
- Cần đổi mật khẩu tài khoản Oracle trên máy chủ, điền chuỗi kết nối mới vào `.env` hoặc kho bí mật của môi trường triển khai, và cấp khóa JWT mới cho môi trường đó. Các giá trị cũ có thể vẫn nằm trong lịch sử Git hoặc bản sao đã phát tán; việc sửa file hiện tại không thu hồi được chúng.
- Các vấn đề khác như mật khẩu người dùng lưu dạng văn bản thuần và quy trình nạp tiền chưa xác minh nguồn tiền vẫn chưa được xử lý.

## 4. Phân quyền theo database (11/11/2026)

- Đọc `backupsql/.sql`: schema đã có `ACCOUNTS.ROLE`, `ACCOUNTS.STATUS` và bảng `ROLES` với hai vai trò `Customer`, `Admin`. Không cần thêm cột role; schema hiện chưa có vai trò nhân viên.
- Đăng nhập đọc role và status từ `ACCOUNTS`; chỉ tài khoản `ACTIVE` với role được hỗ trợ mới nhận JWT. Bỏ cách cấp Admin dựa trên `Authorization:AdminAccounts` trong cấu hình. Mỗi request dùng JWT được đối chiếu lại với role và status hiện tại trong database, nên đổi role hoặc khóa tài khoản sẽ làm token cũ không còn được chấp nhận.
- Đăng ký công khai ghi tường minh `STATUS='ACTIVE'`, `ROLE='Customer'`; request không thể tự chọn role.
- `CustomerOnly`: các API tự xem tài khoản, rút tiền và chuyển tiền. `AdminOnly`: API nạp tiền `POST /Transaction/{accountNumber}/Deposit`.
- `AccountAccess`: `GET /Account/{accountNumber}/AccountInfo`, `/Balance`, `/TransactionHistory`. Customer chỉ truy cập số tài khoản của mình; Admin được truy cập các số tài khoản khác. Policy từ chối khi route thiếu `accountNumber`.
- Endpoint nạp tiền cũ `POST /Transaction/Deposit` đã được thay bằng endpoint Admin ở trên. `Login` và `Register` vẫn là API công khai.
- Build lại thành công với 0 lỗi, 0 cảnh báo. Chưa xác minh luồng có token với Oracle thật vì chuỗi kết nối local đang để trống.
- `backupsql/` được Git bỏ qua và không được đưa vào bản zip vì bản export chứa mật khẩu mẫu và dữ liệu cá nhân.
