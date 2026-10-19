using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using HuanBank.Api.Services;
using HuanBank.Api.Model.Request;
using HuanBank.Api.Result;
namespace HuanBank.Api.Controller;

[ApiController]
[Route("[controller]")]
[Authorize]
public class TransactionController : ControllerBase
{
    private readonly IBankService _qlnh;
    private readonly ILogger<TransactionController> _logger;
    public TransactionController(IBankService _qlnh, ILogger<TransactionController> logger)
    {
        this._qlnh = _qlnh;
        _logger = logger;
    }
    private string CurrentStk => User.FindFirst(ClaimTypes.NameIdentifier)!.Value;

    [HttpPost("Deposit")]
    public IActionResult Deposit([FromBody] DepositRequest req)
    {
        try
        {
            var kq = _qlnh.Deposit(CurrentStk, req.Amount);
            return kq switch
            {
                TransactionResult.AccountNotFound =>
               NotFound(new { message = "Khong tim thay tai khoan" }),
                TransactionResult.InvalidAmount =>
                    BadRequest(new { message = "So tien khong hop le" }),
                TransactionResult.Success =>
                    Ok(new { message = "Nap tien thanh cong", soDu = _qlnh.GetBalance(CurrentStk) }),
                _ => StatusCode(500, new { message = "Loi he thong" })
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Nap tien - Loi he thong khi STK {AccountNumber} nap tien", CurrentStk);
            return StatusCode(500, new { message = "Loi he thong" });
        }
    }
    [HttpPost("Withdraw")]
    public IActionResult Withdraw([FromBody] WithdrawRequest req)
    {
        try
        {
            var kq = _qlnh.Withdraw(CurrentStk, req.Amount);
            return kq switch
            {
                TransactionResult.Success =>
                Ok(new { message = "Rut tien thanh cong" }),
                TransactionResult.AccountNotFound =>
                    NotFound(new { message = "Khong tim thay tai khoan" }),
                TransactionResult.InvalidAmount =>
                    BadRequest(new { message = "So tien khong hop le" }),
                TransactionResult.InsufficientBalance =>
                    BadRequest(new { message = "So du khong du" }),
                _ => StatusCode(500, new { message = "Loi he thong" })
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Rut tien - Loi he thong khi STK {AccountNumber} rut tien", CurrentStk);
            return StatusCode(500, new { message = "Loi he thong" });
        }
    }
    [HttpPost("TransferMoney")]
    public IActionResult TransferMoney([FromBody] TransferRequest req)
    {
        try
        {
            var kq = _qlnh.TransferMoney(CurrentStk, req.CounterAccount, req.Amount);
            return kq switch
            {
                TransactionResult.Success =>
                Ok(new { message = "Chuyen tien thanh cong" }),
                TransactionResult.AccountNotFound =>
                    NotFound(new { message = "Khong tim thay tai khoan" }),
                TransactionResult.InvalidAmount =>
                    BadRequest(new { message = "So tien khong hop le" }),
                TransactionResult.InsufficientBalance =>
                    BadRequest(new { message = "So du khong du" }),
                TransactionResult.CannotTransferToSelf =>
                    BadRequest(new { message = "Khong the chuyen tien cho chinh minh" }),
                _ => StatusCode(500, new { message = "Loi he thong" })
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chuyen tien - Loi he thong khi STK {AccountNumber1} chuyen tien cho {AccountNumber2}", CurrentStk, req.CounterAccount);
            return StatusCode(500, new { message = "Loi he thong" });
        }
    }
}
