# BÁO CÁO CÁC THAY ĐỔI TRONG DỰ ÁN HUANBANK.API

**Ngày cập nhật:** 16/11/2026
**Phạm vi:** cấu trúc enum và validator, kết quả giao dịch, cấu hình bí mật và phân quyền API.

## I. Nội dung thay đổi

### 1. Tách enum và validator theo nghiệp vụ

Trước đây, enum `TransactionResult` chứa chung kết quả đăng ký, đăng nhập và giao dịch. Class `AccountValidator` cũng xử lý nhiều loại kiểm tra trong cùng một nơi. Cấu trúc mới tách validator đăng ký thành `IRegisterAccountValidator` và `RegisterAccountValidator`, validator giao dịch tiền thành `IMoneyTransactionValidator` và `MoneyTransactionValidator`. Các enum dùng cho bước kiểm tra đầu vào được đặt trong [`ValidatorResult.cs`](../enum/ValidatorResult.cs); kết quả xử lý đăng ký và đăng nhập của service nằm trong [`ServiceResults.cs`](../enum/ServiceResults.cs).

`RegisterAccountValidator` kiểm tra các trường bắt buộc và định dạng ngày sinh. `MoneyTransactionValidator` kiểm tra số tiền phải lớn hơn 0 và trường hợp chuyển tiền cho chính tài khoản nguồn. Hai validator được đăng ký qua dependency injection để [`BankService`](../service/BankService.cs) sử dụng. Việc kiểm tra đủ số dư vẫn nằm trong luồng DAO có giao dịch database nhằm bảo đảm cập nhật số dư nhất quán.

### 2. Điều chỉnh kết quả giao dịch theo mã tham khảo

Trong `settling_service.zip`, một số phương thức service trả đối tượng có `Code` và `Message`; controller đọc `Code` để chọn phản hồi HTTP. Dự án áp dụng ý tưởng này cho `Deposit`, `Withdraw` và `TransferMoney` qua [`BusinessResponse`](../service/Models/BusinessResponse.cs), thay cho enum `TransactionResult`. `TransactionCodes` được bổ sung để đặt tên cho các mã nghiệp vụ `1`, `-1`, `-2`, `-3`, `-4`; class này không có trong mã tham khảo.

[`TransactionController`](../Controllers/TransactionController.cs) dùng `Code` để trả HTTP 200 khi thành công, 400 khi dữ liệu hoặc số dư không hợp lệ, và 404 khi không tìm thấy tài khoản. `Message` được đưa vào JSON phản hồi cho người gọi API; trường này không tự động ghi vào log. `RegistrationResult` và `LoginResult` vẫn là enum vì thay đổi sang `BusinessResponse` chỉ áp dụng cho ba nghiệp vụ giao dịch.

### 3. Chuyển thông tin bí mật ra khỏi mã nguồn

Khóa ký JWT đã được bỏ khỏi `appsettings.json`; chuỗi kết nối Oracle cố định đã được bỏ khỏi [`Connectdb.cs`](../util/Connectdb.cs). Ứng dụng hiện đọc `Jwt__Key` và `ConnectionStrings__Oracle` từ cấu hình. Trong môi trường `Development`, [`Program.cs`](../main/Program.cs) đọc file `.env` ở thư mục project, đổi `__` trong tên khóa thành `:`, rồi đưa giá trị vào `builder.Configuration`. Biến môi trường hệ điều hành có mức ưu tiên cao hơn giá trị trong `.env`.

`Database` nhận cấu hình qua dependency injection. `GetConnectionString("Oracle")` lấy giá trị của khóa `ConnectionStrings:Oracle`; DAO chỉ mở kết nối khi gọi `GetConnection()`. Dự án có `.env.example`, quy tắc Git bỏ qua `.env` và file hướng dẫn `SECURITY_CONFIG.md`. Báo cáo không chứa khóa hoặc mật khẩu thật.

### 4. Áp dụng phân quyền theo database

Schema Oracle đã có `ACCOUNTS.ROLE`, `ACCOUNTS.STATUS` và bảng `ROLES`; không cần thêm cột role. Dữ liệu hiện có hai vai trò `Customer` và `Admin`, chưa có vai trò nhân viên. Cách suy ra Admin từ danh sách số tài khoản trong cấu hình đã được thay bằng việc đọc role và trạng thái từ Oracle khi đăng nhập. Role hợp lệ được đưa vào JWT; tài khoản đăng ký công khai luôn có `ROLE = 'Customer'`.

Khi xác thực request mang JWT, [`Program.cs`](../main/Program.cs) đối chiếu role và trạng thái tài khoản hiện tại trong database với claim trong token. Hai role và ba policy được khai báo tại [`BankRoles.cs`](../Authorization/BankRoles.cs): `CustomerOnly`, `AdminOnly` và `AccountAccess`. `AccountAccess` là policy, không phải role thứ ba. [`AccountAccessHandler`](../Authorization/AccountAccessRequirement.cs) chỉ cho Customer truy cập khi `accountNumber` trên URL bằng số tài khoản trong token; Admin được truy cập tài khoản được chỉ định.

| Nhóm API | Customer | Admin |
| --- | --- | --- |
| `GET /Account/AccountInfo`, `/Balance`, `/TransactionHistory` | Xem tài khoản gắn với token | Không dùng các route này |
| `GET /Account/{accountNumber}/AccountInfo`, `/Balance`, `/TransactionHistory` | Chỉ xem tài khoản gắn với token | Xem tài khoản được chỉ định |
| `POST /Transaction/{accountNumber}/Deposit` | Nạp vào tài khoản gắn với token | Nạp vào tài khoản được chỉ định |
| `POST /Transaction/Withdraw`, `/TransferMoney` | Rút hoặc chuyển từ tài khoản trong token | Không được phép |
| `POST /Auth/Login`, `/Register` | Công khai, không cần token | Công khai, không cần token |

Endpoint `Deposit` hiện gắn policy `AccountAccess`, không phải `AdminOnly`; do đó Customer có thể nạp vào tài khoản gắn với token của Customer. Policy `AdminOnly` đã được khai báo nhưng hiện chưa được gắn cho endpoint nào.
