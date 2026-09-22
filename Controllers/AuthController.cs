using Microsoft.AspNetCore.Mvc;
using HuanBank.Api.Services;
using HuanBank.Api.Request;
namespace HuanBank.Api.Controller;
[ApiController]
[Route("[controller]")]
public class AuthController : ControllerBase
{
    private readonly IBankService _qlnh;
    private readonly TokenService tokenService;

    public AuthController(IBankService _qlnh, TokenService tokenService)
    {
        this._qlnh = _qlnh;
        this.tokenService=tokenService;
    }
    [HttpPost("Login")]
public IActionResult Login([FromBody] LoginRequest req)
{
    var kq = _qlnh.Login(req.stk,req.mk);
    return kq switch
    {
        TransactionResult.Success =>
            Ok(new { message = "Dang nhap thanh cong", token = tokenService.GenerateToken(req.stk) }),
        TransactionResult.AccountNotFound =>
            NotFound(new { message = "Khong tim thay tai khoan" }),
        TransactionResult.IncorrectPassword =>
            BadRequest(new { message = "Sai mat khau" }),
        _ => StatusCode(500)
    };
}
    [HttpPost("Register")]
    public IActionResult Register([FromBody] RegisterRequest req)
    {
        var kq=_qlnh.CreateAccount(req.Name,req.Phone,req.Email,req.Dateofbirth,req.Adress,req.AccountNumber,req.Password);
        return kq switch
        {
            TransactionResult.Success=>
                Created($"/account/{req.AccountNumber}",new{message="Tao tai khoan thanh cong",stk=req.AccountNumber}),
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

// public record LoginRequest(string stk,string mk);