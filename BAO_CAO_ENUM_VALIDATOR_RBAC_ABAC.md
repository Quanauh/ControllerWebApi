# Báo cáo: tách enum, validator, chuẩn hóa kết quả service và triển khai RBAC/ABAC

**Dự án:** `HuanBank.Api`  
**Ngày đối chiếu mã nguồn:** 16/11/2026  
**Phạm vi:** mã nguồn hiện có, lịch sử Git của dự án, `backupsql/.sql` và mã tham khảo trong `C:\Users\Admin\settling_service.zip`.

> Báo cáo mô tả trạng thái mã nguồn tại thời điểm đối chiếu. File SQL backup chứa dữ liệu cá nhân và mật khẩu mẫu; báo cáo không chép lại các giá trị đó. Một số thay đổi trong working tree chưa được commit, đặc biệt là policy của endpoint `Deposit`.

## Phần I. Tách enum và validator; điều chỉnh kết quả service theo mã mẫu

### 1. Tình trạng ban đầu và lý do tách

Lịch sử Git tại commit `9588977` cho thấy dự án dùng một enum `TransactionResult` cho nhiều loại kết quả: đăng ký, đăng nhập và giao dịch tiền. Chẳng hạn, cùng enum này có `InvalidDate`, `IncorrectPassword`, `InvalidAmount` và `InsufficientBalance`. Một class `AccountValidator` cùng interface `IAccountValidator` xử lý cả đăng ký, kiểm tra số tiền, rút và chuyển tiền. Cách gom này khiến mỗi luồng phụ thuộc vào các giá trị kết quả và phương thức không liên quan đến mình.

Commit `525ee8c` tách theo **trách nhiệm nghiệp vụ**, chứ không phải tạo một file cho từng giá trị enum:

| Trước | Sau | Mục đích |
| --- | --- | --- |
| `enum/TransactionResult.cs` chứa một enum cho nhiều luồng | [`enum/ValidatorResult.cs`](enum/ValidatorResult.cs) chứa `RegistrationValidationResult`, `MoneyTransactionValidationResult`; [`enum/ServiceResults.cs`](enum/ServiceResults.cs) chứa `RegistrationResult`, `LoginResult` | Phân biệt kết quả kiểm tra đầu vào với kết quả nghiệp vụ của service |
| `Validation/IValidator.cs`, `Validation/Validator.cs` kiểm tra nhiều nghiệp vụ | [`IRegisterAccountValidator`](Validation/IRegisterAccountValidator.cs) và [`RegisterAccountValidator`](Validation/RegisterAccountValidator.cs) | Tập trung kiểm tra dữ liệu đăng ký |
| Cùng validator xử lý cả tiền | [`IMoneyTransactionValidator`](Validation/IMoneyTransactionValidator.cs) và [`MoneyTransactionValidator`](Validation/MoneyTransactionValidator.cs) | Tập trung kiểm tra số tiền và trường hợp chuyển cho chính mình |

`RegisterAccountValidator` kiểm tra trường bắt buộc và ngày sinh theo định dạng `dd/MM/yyyy`. `MoneyTransactionValidator` từ chối số tiền không dương và so sánh tài khoản nguồn/đích khi chuyển tiền. Kiểm tra đủ số dư vẫn được xử lý trong luồng DAO có giao dịch database; validator hiện không tự đọc số dư. Hai validator được đăng ký qua dependency injection trong [`Program.cs`](main/Program.cs) và được [`BankService`](service/BankService.cs) sử dụng. Vì vậy, sửa quy tắc kiểm tra đăng ký không phải sửa class kiểm tra giao dịch tiền.

### 2. Đối chiếu với `settling_service.zip`

Mã tham khảo có `SettlingEngine/Models/BusinessReponse.cs` (tên trong mẫu viết là `Reponse`) với hai thuộc tính `Code` và `Message`. `ISettlingService` và `SettlingService` trả đối tượng này từ một số phương thức; service điền mã và thông điệp cho từng kết quả. `SettlingController` đọc `response.Code` để quyết định phản hồi HTTP, ví dụ mã dương trả `Ok`, mã không dương trả `BadRequest`.

Dự án HuanBank áp dụng **ý tưởng trả đối tượng có mã và thông điệp**, không sao chép nguyên nghiệp vụ settling, tên class viết sai trong mẫu, hay việc dùng `Task`/`async`. Ba phương thức giao dịch của HuanBank vẫn đồng bộ và vẫn dùng `IAccountDAO` hiện tại.

| Thành phần HuanBank | Điều chỉnh |
| --- | --- |
| [`BusinessResponse`](service/Models/BusinessResponse.cs) | `record` có `long Code` và `string Message`; tên `Response` được viết đúng chính tả |
| `TransactionCodes` trong cùng file | Đặt tên cho các mã: thành công `1`, không có tài khoản `-1`, số tiền sai `-2`, thiếu số dư `-3`, chuyển cho chính mình `-4` |
| [`IBankService`](service/IBankService.cs) | `Deposit`, `Withdraw`, `TransferMoney` trả `BusinessResponse` thay vì enum `TransactionResult` |
| [`BankService`](service/BankService.cs) | Mỗi nhánh kết quả tạo `BusinessResponse(Code, Message)` tương ứng; enum kết quả giao dịch cũ bị bỏ |
| [`TransactionController`](Controllers/TransactionController.cs) | Đọc `Code` để chọn HTTP 200, 400 hoặc 404; dữ liệu trả về có `code` và `message` |

Ví dụ, số tiền không hợp lệ tạo `BusinessResponse` với `Code = -2`; controller trả HTTP 400. Không tìm thấy tài khoản dùng `Code = -1` và HTTP 404. Thành công dùng `Code = 1` và HTTP 200. **`Code` là mã nghiệp vụ, không phải mã HTTP.** Với nạp tiền thành công, controller còn trả `soDu`. Các ngoại lệ không dự kiến vẫn được controller ghi log và trả HTTP 500.

`RegistrationResult` và `LoginResult` vẫn là enum vì lần điều chỉnh theo mẫu tập trung vào **ba phương thức giao dịch**. Sau khi triển khai phân quyền, `LoginResult` được bổ sung trạng thái tài khoản không hoạt động và role không hợp lệ; service trả [`LoginOutcome`](service/Models/LoginOutcome.cs) gồm `LoginResult` và role cần đưa vào token.

### 3. Đánh giá phần I

Việc tách validator và nhóm enum làm ranh giới giữa kiểm tra đầu vào, kết quả service và phản hồi HTTP rõ hơn. Việc trả `BusinessResponse` giúp service truyền thông điệp cụ thể cho controller. Tuy nhiên, dự án hiện dùng cả enum (`RegistrationResult`, `LoginResult`) và `BusinessResponse` cho các luồng khác nhau; đây là trạng thái có chủ đích theo phạm vi đã sửa, không phải mọi service đều đã chuyển sang mẫu `Code`/`Message`.

## Phần II. Tìm hiểu RBAC/ABAC và áp dụng vào HuanBank.Api

### 1. Khái niệm và cách kết hợp

**RBAC** (Role-Based Access Control) gán quyền cho vai trò rồi gán vai trò cho người dùng. Trong dự án này, ví dụ vai trò là `Customer` và `Admin`; policy `CustomerOnly` hoặc `AdminOnly` kiểm tra vai trò trước khi cho chạy endpoint. [NIST mô tả RBAC qua quan hệ giữa người dùng, vai trò và quyền](https://csrc.nist.gov/projects/role-based-access-control/faqs).

**ABAC** (Attribute-Based Access Control) ra quyết định dựa trên thuộc tính của người yêu cầu, tài nguyên, thao tác và có thể cả bối cảnh môi trường. HuanBank hiện áp dụng một quy tắc thuộc tính hẹp: so sánh số tài khoản trong token của Customer với `accountNumber` trên route; Admin được phép chọn tài khoản khác. Dự án chưa kiểm tra thêm các thuộc tính như thời gian, vị trí hay hạn mức giao dịch trong handler. [NIST SP 800-162](https://csrc.nist.gov/pubs/sp/800/162/upd2/final) là nguồn định nghĩa ABAC được dùng ở đây.

`AccountAccess` là **policy**, không phải role thứ ba. Trong ASP.NET Core, `[Authorize(Policy = "...")]` chọn policy đã đăng ký; framework đánh giá các requirement và gọi handler tương ứng. [Tài liệu Microsoft về policy](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/policies?view=aspnetcore-10.0) mô tả cơ chế này.

### 2. Dữ liệu quyền trong Oracle

File [`backupsql/.sql`](backupsql/.sql) cho thấy `ACCOUNTS` đã có `ROLE` và `STATUS`; `ROLE` tham chiếu bảng `ROLES`. Dữ liệu mẫu có `Customer` và `Admin`, chưa có role `Employee`. Vì vậy, đợt triển khai này **không thêm cột role hoặc tự tạo quyền nhân viên**.

Trước khi sửa, [`TokenService`](util/TokenService.cs) xác định Admin từ danh sách `Authorization:AdminAccounts` trong cấu hình. Cách đó không dùng `ACCOUNTS.ROLE` và dễ lệch với dữ liệu Oracle. Hiện tại:

1. [`AccountDAO`](DAO/AccountDAO.cs) đọc `PASSWORD`, `ROLE`, `STATUS` của tài khoản khi đăng nhập.
2. [`BankService.Login`](service/BankService.cs) xác nhận mật khẩu, yêu cầu `STATUS = 'ACTIVE'` và role thuộc `Customer` hoặc `Admin`; kết quả thành công đi kèm role lấy từ database.
3. [`AuthController`](Controllers/AuthController.cs) truyền role đó cho `TokenService.GenerateToken`, nơi JWT nhận claim `NameIdentifier` và claim role. Danh sách `AdminAccounts` đã bị bỏ.
4. Mỗi khi JWT được xác thực, `OnTokenValidated` trong `Program.cs` đọc lại role của tài khoản `ACTIVE` từ DAO. Nếu tài khoản không còn hoạt động hoặc role hiện tại khác claim trong token, request không được xác thực. Điều này giúp thay role hoặc khóa tài khoản có hiệu lực với token đã phát hành, với điều kiện API truy cập được Oracle.
5. Đăng ký công khai ghi tường minh `ROLE = 'Customer'` và `STATUS = 'ACTIVE'`; request đăng ký không có trường chọn role. Không có API công khai để tự nâng lên Admin.

Khóa ký JWT và chuỗi kết nối Oracle đã được chuyển khỏi mã nguồn sang cấu hình môi trường; hướng dẫn ở [`SECURITY_CONFIG.md`](SECURITY_CONFIG.md). Đây là phần hỗ trợ cho access control: nếu khóa JWT bị lộ, claim role và số tài khoản có thể bị giả mạo.

### 3. Các policy và kiểm tra tài khoản đích

[`BankRoles.cs`](Authorization/BankRoles.cs) khai báo hai role và ba tên policy. [`Program.cs`](main/Program.cs) đăng ký chúng:

| Policy | Quy tắc |
| --- | --- |
| `CustomerOnly` | Role trong token là `Customer` |
| `AdminOnly` | Role trong token là `Admin` |
| `AccountAccess` | Đã đăng nhập, role là `Customer` **hoặc** `Admin`, và vượt qua `AccountAccessRequirement` |

[`AccountAccessHandler`](Authorization/AccountAccessRequirement.cs) lấy `accountNumber` từ route. Thiếu giá trị này thì requirement không thành công. Nếu là Admin, requirement thành công; nếu là Customer, số tài khoản trong claim `NameIdentifier` phải bằng `accountNumber`. Việc so sánh dùng `StringComparison.Ordinal`. Đây là nơi thực thi quy tắc sở hữu tài khoản; `RequireRole(Customer, Admin)` một mình chưa đủ để giới hạn Customer vào tài khoản của họ.

### 4. Ma trận API theo **mã hiện tại**

| API | Policy | Customer | Admin |
| --- | --- | --- | --- |
| `POST /Auth/Login`, `POST /Auth/Register` | Không yêu cầu token | Gọi được | Gọi được |
| `GET /Account/AccountInfo`, `/Balance`, `/TransactionHistory` | `CustomerOnly` | Xem tài khoản trong token | Bị từ chối trên các route tự xem này |
| `GET /Account/{accountNumber}/AccountInfo`, `/Balance`, `/TransactionHistory` | `AccountAccess` | Chỉ khi `accountNumber` là tài khoản trong token | Xem được tài khoản được chỉ định |
| `POST /Transaction/{accountNumber}/Deposit` | **`AccountAccess` trong working tree hiện tại** | Nạp vào tài khoản của mình | Nạp vào tài khoản được chỉ định |
| `POST /Transaction/Withdraw`, `POST /Transaction/TransferMoney` | `CustomerOnly` | Tài khoản nguồn lấy từ token | Bị từ chối |

`AccountController` dùng các hàm private `GetAccountInfoCore`, `GetBalanceCore`, `GetTransactionHistoryCore` để hai kiểu route cùng gọi `BankService` mà không lặp mã tạo response. Các hàm `Core` không tự quyết định quyền; policy của endpoint kiểm tra quyền trước khi action chạy.

**Lưu ý về thay đổi chưa commit:** `TransactionController` hiện gắn `AccountAccess` cho `Deposit`, thay cho `AdminOnly` trong commit gần nhất và báo cáo cũ. Do đó Customer hiện có thể tự nạp tiền vào số dư của mình. Đây là rủi ro nghiệp vụ nếu API được dùng với tiền thật, vì code chưa xác minh nguồn tiền hoặc giao dịch nạp từ hệ thống thanh toán. `AdminOnly` vẫn được đăng ký nhưng hiện không thấy endpoint nào gắn policy đó.

### 5. Kiểm tra và giới hạn

[`tests/AccessControlSmoke`](tests/AccessControlSmoke/Program.cs) dùng `StubDao` để kiểm tra Customer chỉ qua với tài khoản của mình, Admin qua với tài khoản được chỉ định, route thiếu tài khoản bị từ chối; kiểm tra thêm đăng nhập với role Customer/Admin, tài khoản khóa, role lạ và mật khẩu sai. Đây là kiểm tra logic trực tiếp, **không** chạy toàn bộ API hoặc kết nối Oracle. Những lần build API trước khi lập báo cáo thành công; báo cáo này chỉ đối chiếu mã, không thực hiện lại kiểm thử triển khai.

Các giới hạn còn lại: mật khẩu người dùng trong schema/DAO vẫn ở dạng văn bản thuần; file SQL backup có dữ liệu nhạy cảm và được giữ ngoài Git/bản zip; chưa có vai trò nhân viên; chưa có phép thử tích hợp với Oracle thật. Nếu cần dùng cho hệ thống ngân hàng thực tế, cần ưu tiên băm mật khẩu, kiểm soát nghiệp vụ nạp tiền và kiểm thử phân quyền ở mức HTTP với database thử nghiệm.
