using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using HuanBank.Api.Services;
using HuanBank.Api.Services.Models;
using HuanBank.Api.Model.Request;
using HuanBank.Api.Result;
using HuanBank.Api.Authorization;
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

    [HttpPost("{accountNumber}/Deposit")]
    [Authorize(Policy = BankPolicies.AccountAccess)]
    public IActionResult Deposit(string accountNumber, [FromBody] DepositRequest req)
    {
        try
        {
            var kq = _qlnh.Deposit(accountNumber, req.Amount);
            return kq switch
            {
                { Code: TransactionCodes.AccountNotFound } => NotFound(kq),
                { Code: TransactionCodes.InvalidAmount } => BadRequest(kq),
                { Code: TransactionCodes.Success } =>
                    Ok(new { kq.Code, kq.Message, soDu = _qlnh.GetBalance(accountNumber) }),
                _ => StatusCode(500, new { message = "Loi he thong" })
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Nap tien - Loi he thong khi STK {AccountNumber} nap tien", accountNumber);
            return StatusCode(500, new { message = "Loi he thong" });
        }
    }
    [HttpPost("Withdraw")]
    [Authorize(Policy = BankPolicies.CustomerOnly)]
    public IActionResult Withdraw([FromBody] WithdrawRequest req)
    {
        try
        {
            var kq = _qlnh.Withdraw(CurrentStk, req.Amount);
            return kq switch
            {
                { Code: TransactionCodes.Success } => Ok(kq),
                { Code: TransactionCodes.AccountNotFound } => NotFound(kq),
                { Code: TransactionCodes.InvalidAmount or TransactionCodes.InsufficientBalance } => BadRequest(kq),
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
    [Authorize(Policy = BankPolicies.CustomerOnly)]
    public IActionResult TransferMoney([FromBody] TransferRequest req)
    {
        try
        {
            var kq = _qlnh.TransferMoney(CurrentStk, req.CounterAccount, req.Amount);
            return kq switch
            {
                { Code: TransactionCodes.Success } => Ok(kq),
                { Code: TransactionCodes.AccountNotFound } => NotFound(kq),
                { Code: TransactionCodes.InvalidAmount or TransactionCodes.InsufficientBalance or TransactionCodes.CannotTransferToSelf } => BadRequest(kq),
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
