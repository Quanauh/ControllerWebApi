using Microsoft.AspNetCore.Mvc;
using HuanBank.Api.Services;
using HuanBank.Api.Model.Responses;
using Microsoft.AspNetCore.Authorization;
using HuanBank.Api.Authorization;
namespace HuanBank.Api.Controller;

[ApiController]
[Route("[controller]")]
public class AccountController : ControllerBase
{
    private readonly IBankService _qlnh;
    private readonly ILogger<AccountController> _logger;

    public AccountController(IBankService _qlnh, ILogger<AccountController> logger)
    {
        this._qlnh = _qlnh;
        _logger = logger;
    }
    [HttpGet("{accountNumber}/AccountInfo")]
    [Authorize(Policy = BankPolicies.AccountAccess)]
    [Authorize(Policy = BankPermissions.ViewAccount)]
    public IActionResult GetAccountInfoByNumber(string accountNumber) => GetAccountInfoCore(accountNumber);

    private IActionResult GetAccountInfoCore(string accountNumber)
    {
        try
        {
            if (!_qlnh.AccountExists(accountNumber))
                return NotFound(new { message = "Khong tim thay tai khoan" });

            var kq = _qlnh.GetAccountInfor(accountNumber);
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
            _logger.LogError(ex, "Lay thong tin - Loi he thong khi lay thong tin {AccountNumber}", accountNumber);
            return StatusCode(500, new { message = "Loi he thong" });
        }
    }
    [HttpGet("{accountNumber}/Balance")]
    [Authorize(Policy = BankPolicies.AccountAccess)]
    [Authorize(Policy = BankPermissions.ViewBalance)]
    public IActionResult GetBalanceByNumber(string accountNumber) => GetBalanceCore(accountNumber);

    private IActionResult GetBalanceCore(string accountNumber)
    {
        try
        {
            if (!_qlnh.AccountExists(accountNumber))
                return NotFound(new { message = "Khong tim thay tai khoan" });

            var kq = _qlnh.GetBalance(accountNumber);
            return Ok(new BalanceResponse
            {
                Stk = accountNumber,
                SoDu = kq
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Xem so du - Loi he thong khi lay so du {AccountNumber}", accountNumber);
            return StatusCode(500, new { message = "Loi he thong" });
        }
    }
    [HttpGet("{accountNumber}/TransactionHistory")]
    [Authorize(Policy = BankPolicies.AccountAccess)]
    [Authorize(Policy = BankPermissions.ViewHistory)]
    public IActionResult GetTransactionHistoryByNumber(string accountNumber) => GetTransactionHistoryCore(accountNumber);

    private IActionResult GetTransactionHistoryCore(string accountNumber)
    {
        try
        {
            if (!_qlnh.AccountExists(accountNumber))
                return NotFound(new { message = "Khong tim thay tai khoan" });

            var kq = _qlnh.GetTransactionHistory(accountNumber);
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
            _logger.LogError(ex, "Lay lich su giao dich - Loi he thong khi lay lich su giao dich {AccountNumber}", accountNumber);
            return StatusCode(500, new { message = "Loi he thong" });
        }
    }
}
