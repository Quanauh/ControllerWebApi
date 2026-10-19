using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using HuanBank.Api.Services;
using HuanBank.Api.Model.Responses;
using Microsoft.AspNetCore.Authorization;
namespace HuanBank.Api.Controller;

[ApiController]
[Route("[controller]")]
[Authorize]

public class AccountController : ControllerBase
{
    private readonly IBankService _qlnh;
    private readonly ILogger<AccountController> _logger;

    public AccountController(IBankService _qlnh, ILogger<AccountController> logger)
    {
        this._qlnh = _qlnh;
        _logger = logger;
    }
    private string CurrentStk => User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
    [HttpGet("AccountInfo")]
    public IActionResult GetAccountInfo()
    {
        try
        {
            if (!_qlnh.AccountExists(CurrentStk))
                return NotFound(new { message = "Khong tim thay tai khoan" });

            var kq = _qlnh.GetAccountInfor(CurrentStk);
            if (kq == null) return NotFound(new { message = "Khong tim thay tai khoan" });

            return Ok(new AccountInfoResponse
            {
                AccountNumber = kq.AccountNumber,
                FullName = kq.FullName,
                Phone = kq.Phone,
                Email = kq.Email,
                DateOfBirth = kq.DateOfBirth?.ToString("dd/MM/yyyy"),
                Address = kq.Address,
                Balance = kq.Balance
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lay thong tin - Loi he thong khi lay thong tin {AccountNumber}", CurrentStk);
            return StatusCode(500, new { message = "Loi he thong" });
        }
    }
    [HttpGet("Balance")]
    public IActionResult GetBalance()
    {
        try
        {
            if (!_qlnh.AccountExists(CurrentStk))
                return NotFound(new { message = "Khong tim thay tai khoan" });

            var kq = _qlnh.GetBalance(CurrentStk);
            return Ok(new BalanceResponse
            {
                Stk = CurrentStk,
                SoDu = kq
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Xem so du - Loi he thong khi lay so du {AccountNumber}", CurrentStk);
            return StatusCode(500, new { message = "Loi he thong" });
        }
    }
    [HttpGet("TransactionHistory")]
    public IActionResult GetTransactionHistory()
    {
        try
        {
            if (!_qlnh.AccountExists(CurrentStk))
                return NotFound(new { message = "Khong tim thay tai khoan" });

            var kq = _qlnh.GetTransactionHistory(CurrentStk);
            List<TransactionHistoryResponse> response = kq.Select(gd => new TransactionHistoryResponse
            {
                AccountNumber = gd.AccountNumber,
                CounterAccount = gd.CounterAccount,
                TransactionType = gd.TransactionType,
                Amount = gd.Amount,
                BalanceBefore = gd.BalanceBefore,
                BalanceAfter = gd.BalanceAfter,
                TransactionDate = gd.TransactionDate,
                Direction = gd.Direction
            }).ToList();

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lay lich su giao dich - Loi he thong khi lay lich su giao dich {AccountNumber}", CurrentStk);
            return StatusCode(500, new { message = "Loi he thong" });
        }
    }
}
