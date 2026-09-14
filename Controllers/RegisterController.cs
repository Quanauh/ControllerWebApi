using Microsoft.AspNetCore.Mvc;
[ApiController]
[Route("[controller]")]
public class RegisterController : ControllerBase
{
    private readonly BankManager qlnh;
    public RegisterController(BankManager qlnh)
    {
        this.qlnh=qlnh;
    }
    [HttpPost]
    public IActionResult Register([FromBody] RgRequest req)
    {
        var kq=qlnh.CreateAccount(req.name,req.phone,req.email,req.date,req.adress,req.accountnumber,req.pw);
        return kq switch
        {
            TransactionResult.Success=>
                Created($"/account/{req.accountnumber}",new{message="Tao tai khoan thanh cong",stk=req.accountnumber}),
            TransactionResult.IsNull=>
            BadRequest(new { message = "Khong duoc de trong thong tin" }),
        TransactionResult.AccountAlreadyExists=>
            Conflict(new {message="So tai khoan da ton tai"}),
        TransactionResult.PhoneAlreadyExists=>
            Conflict(new{message="So dien thoai da ton tai"}),
        TransactionResult.EmailAlreadyExists=>
            Conflict(new{message="email da ton tai"}),
        TransactionResult.InvalidDate=>
            BadRequest(new{message="Ngay sinh khong hop le"}),
        _ => StatusCode(500)
        };
    }
}
public record RgRequest(string name,string phone,string email, string date,string adress,string accountnumber,string pw);