using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("[controller]")]
public class LoginController : ControllerBase
{
    private readonly BankManager qlnh;

    public LoginController(BankManager qlnh)
    {
        this.qlnh = qlnh;
    }
    [HttpPost]
    public IActionResult Login([FromBody] LoginRequest req)
    {
        var kq=qlnh.Login(req.Stk,req.Mk);
        return kq switch
        {
            TransactionResult.Success=>
                Ok(new {message="Dang nhap thanh cong"}),
            TransactionResult.AccountNotFound=>
                NotFound(new{ message="Khong tim thay tai khoan"}),
            TransactionResult.IncorrectPassword=>
                BadRequest(new{message="Mat khau khong dung"}),
                _=> StatusCode(500)
        };
    }
}

public record LoginRequest(string Stk, string Mk);