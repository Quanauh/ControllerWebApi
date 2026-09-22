using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using HuanBank.Api.Services;
using HuanBank.Api.Request;
namespace HuanBank.Api.Controller;
[ApiController]
[Route("[controller]")]
[Authorize]
public class TransactionController : ControllerBase
{
    private readonly IBankService _qlnh;
    public TransactionController(IBankService _qlnh)
    {
        this._qlnh=_qlnh;
    }
        private string CurrentStk => User.FindFirst(ClaimTypes.NameIdentifier)!.Value;

    [HttpPost("Deposit")]
    public IActionResult Deposit([FromBody] DepositRequest req)
    {
        var kq=_qlnh.Deposit(CurrentStk,req.Amount);
        return kq switch
        {
             TransactionResult.AccountNotFound =>
            NotFound(new { message = "Khong tim thay tai khoan" }),
        TransactionResult.InvalidAmount=>
            BadRequest(new{message = "So tien khong hop le"}),
        TransactionResult.Success =>
            Ok(new {message="Nap tien thanh cong",soDu = _qlnh.GetBalance(CurrentStk)}),
            _=>StatusCode(500)
        };
    }
    [HttpPost("Withdraw")]
    public IActionResult Withdraw([FromBody] WithdrawRequest req)
    {
        var kq=_qlnh.Withdraw(CurrentStk,req.Amount);
        return kq switch
        {
            TransactionResult.Success=>
            Ok(new{message = "Rut tien thanh cong"}),
            TransactionResult.AccountNotFound=>
                NotFound(new{ message="Khong tim thay tai khoan"}),
            TransactionResult.InvalidAmount=>
                BadRequest(new{message = "So tien khong hop le"}),
            TransactionResult.InsufficientBalance=>
                BadRequest(new{message = "So du khong du"}),
            _=>StatusCode(500)  
        };
    }
    [HttpPost("TransferMoney")]
    public IActionResult TransferMoney([FromBody] TranformerRequest req)
    {
        var kq = _qlnh.TransferMoney(CurrentStk, req.CounterAccount, req.Amount);
        return kq switch
        {
            TransactionResult.Success=>
            Ok(new {message="Chuyen tien thanh cong"}),
        TransactionResult.AccountNotFound=>
            NotFound(new {message="Khong tim thay tai khoan"}),
        TransactionResult.InvalidAmount=>
            BadRequest(new {message="So tien khong hop le"}) ,
        TransactionResult.InsufficientBalance=>
            BadRequest(new{message = "So du khong du"}),
        TransactionResult.CannotTransferToSelf=>
            BadRequest(new { message = "Khong the chuyen tien cho chinh minh" }),
        _=> StatusCode(500)
        };
    }
}
