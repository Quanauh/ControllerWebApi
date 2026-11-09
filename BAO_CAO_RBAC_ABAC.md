# Báo cáo kiểm tra RBAC và ABAC của HuanBank.Api

## Kết luận

Dự án có xác thực JWT, gán vai trò `Customer` hoặc `Admin`, và yêu cầu policy `AccountAccess` cho các API tài khoản, giao dịch. Các API hiện tại lấy tài khoản nguồn từ claim trong token nên khách hàng thông thường chỉ thao tác trên tài khoản của mình. Tuy nhiên, quy tắc ABAC trong handler chưa thực sự kiểm tra đối tượng tài khoản ở các endpoint hiện có; quyền `Admin` được mô tả là truy cập mọi tài khoản nhưng chưa có API nào thực hiện điều đó. Vì vậy, RBAC mới ở mức nền tảng, còn ABAC chưa đạt mục tiêu ghi trong mã nguồn.

Đã sửa lỗi biên dịch ở `main/Program.cs` bằng cách thêm `using Microsoft.AspNetCore.Authorization;`. Lệnh `dotnet build --no-restore` thành công với 0 lỗi, 0 cảnh báo.

## Phạm vi và luồng phân quyền

| Nhóm API | Xác thực và policy | Tài khoản được sử dụng | Kết quả hiện tại |
| --- | --- | --- | --- |
| `POST /Auth/Login`, `POST /Auth/Register` | Không yêu cầu token | Dữ liệu từ request | Công khai theo thiết kế hiện tại |
| `GET /Account/AccountInfo`, `Balance`, `TransactionHistory` | `AccountAccess` | `NameIdentifier` trong token | Chỉ xem tài khoản của token |
| `POST /Transaction/Deposit`, `Withdraw` | `AccountAccess` | `NameIdentifier` trong token | Chỉ tác động tài khoản của token |
| `POST /Transaction/TransferMoney` | `AccountAccess` | Tài khoản nguồn từ token; tài khoản nhận từ request | Chỉ chuyển tiền từ tài khoản của token |

`Program.cs` cấu hình kiểm tra chữ ký, issuer, audience, thời hạn JWT và policy yêu cầu người dùng đã đăng nhập, có vai trò `Customer` hoặc `Admin`, đồng thời thỏa `AccountAccessRequirement`. `TokenService.cs` gán `Admin` nếu số tài khoản nằm trong `Authorization:AdminAccounts`; trường hợp còn lại là `Customer`. `AccountAccessHandler` cho `Admin` qua ngay, còn `Customer` so sánh số tài khoản trong token với route `accountNumber` nếu có.

## Phát hiện cần xử lý

### 1. Khóa ký JWT nằm trong cấu hình dự án — nghiêm trọng

`appsettings.json` chứa trực tiếp `Jwt:Key`. Người có khóa này có thể tự ký token chứa vai trò `Admin` hoặc số tài khoản của người khác; cả RBAC lẫn ABAC đều phụ thuộc vào việc token không bị giả mạo. Cần thay khóa đã lộ, chuyển khóa sang biến môi trường hoặc secret store, và không đưa khóa thật vào mã nguồn. Mật khẩu Oracle trong `util/Connectdb.cs` cũng cần xử lý tương tự.

### 2. ABAC hiện tại không kiểm tra tài khoản đích — cao

Các route được bảo vệ không có tham số `accountNumber`. Trong `Authorization/AccountAccessRequirement.cs`, khi route không có tham số này, handler dùng chính `subjectAccount` làm `resourceAccount`, khiến phép so sánh luôn đúng nếu token có `NameIdentifier`. Do đó policy chưa đưa ra quyết định dựa trên tài nguyên độc lập. Việc giới hạn tài khoản hiện nay đến từ `CurrentStk` trong hai controller, không phải phép kiểm ABAC.

Khi bổ sung API nhận số tài khoản từ route hoặc body, cần xác định rõ tài khoản bị truy cập và truyền chính tài nguyên đó vào bước authorize. Không nên mặc định tài nguyên bằng tài khoản trong token khi không tìm thấy tài nguyên cần kiểm tra.

### 3. Quyền Admin chưa khớp với mô tả — trung bình

Chú thích trong handler nói `Admin` có thể truy cập mọi tài khoản, nhưng `AccountController` và `TransactionController` đều dùng `CurrentStk` từ token. Vì vậy token `Admin` hiện vẫn chỉ xem và thao tác trên tài khoản gắn với token. Nếu cần chức năng quản trị tài khoản khác, hãy tạo endpoint và policy riêng, nêu rõ thao tác nào được phép; không nên tự động mở rộng quyền trên mọi thao tác tiền.

### 4. Cấp vai trò Admin dựa trên danh sách số tài khoản — trung bình

`TokenService.cs` quyết định vai trò từ danh sách `Authorization:AdminAccounts`, không từ dữ liệu quyền trong cơ sở dữ liệu. Khi cấu hình thay đổi, token đã phát hành vẫn giữ vai trò cũ đến khi hết hạn. Nếu danh sách chứa số tài khoản chưa đăng ký, endpoint đăng ký công khai có thể cho người khác tạo số tài khoản đó rồi nhận vai trò `Admin` khi đăng nhập. Cần quản lý vai trò bằng dữ liệu được kiểm soát, bảo đảm tài khoản Admin được tạo/cấp quyền qua quy trình riêng, và có cơ chế thu hồi quyền khi cần.

## Thứ tự đề xuất

1. Thay và đưa khóa JWT cùng thông tin kết nối Oracle ra khỏi mã nguồn.
2. Quyết định rõ phạm vi chức năng Admin; tạo policy riêng cho các thao tác quản trị nếu cần.
3. Sửa ABAC để kiểm tra tài khoản đích thực tế ở từng endpoint cần truy cập tài nguyên; trường hợp không xác định được tài nguyên phải từ chối.
4. Bổ sung kiểm thử phân quyền cho token thiếu/sai vai trò, khách hàng truy cập tài khoản khác, Admin, và token hết hạn hoặc bị thu hồi.

## Giới hạn kiểm tra

Kết luận dựa trên mã nguồn và kết quả build. Chưa chạy API với Oracle vì dự án không kèm schema hoặc môi trường cơ sở dữ liệu để xác nhận hành vi khi chạy.
