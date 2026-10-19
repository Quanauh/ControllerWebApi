namespace HuanBank.Api.Result;
public enum TransactionResult
{
    Success,
    //tk tồn tại
    AccountAlreadyExists,
    //ko tìm thấy tk
    AccountNotFound,
    //sai mk
    IncorrectPassword,
    //số tiền ko hợp lệ
    InvalidAmount,
    //số dư không đủ
    InsufficientBalance,
    //ko chuyển tiền cho chính mình
    CannotTransferToSelf,
    //sđt đã tồn tại
    PhoneAlreadyExists,
    //email đã tồn tại
    EmailAlreadyExists,
    //Lỗi hệ thống
    SystemError,
    //Ngày sinh không hợp lệ
    InvalidDate,
    // Thông tin trống
    IsNull
}