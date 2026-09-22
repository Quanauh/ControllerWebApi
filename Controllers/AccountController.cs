using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using  HuanBank.Api.Services;
using Microsoft.AspNetCore.Authorization;
namespace HuanBank.Api.Controller;
    [ApiController]
    [Route("[controller]")]
    [Authorize]

    public class AccountController : ControllerBase
    {
        private readonly IBankService _qlnh;
        
        public AccountController(IBankService _qlnh)
        {
            this._qlnh=_qlnh;
        }
        private string CurrentStk => User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
        [HttpGet("AccountInfo")]
        public IActionResult GetAccountInfo()
        { if (!_qlnh.AccountExists(CurrentStk))
            return NotFound(new { message = "Khong tim thay tai khoan" });

        var kq = _qlnh.GetAccountInfor(CurrentStk);
        if (kq == null) return StatusCode(500);

        return Ok(new {
            kq.AccountNumber,
            kq.FullName,
            kq.Phone,
            kq.Email,
            DateOfBirth = kq.DateOfBirth?.ToString("dd/MM/yyyy"),
            kq.Address,
            kq.Balance});
        }
        [HttpGet("Balance")]
    public IActionResult GetBalance()
    {
            var kq=_qlnh.GetBalance(CurrentStk);
            if (!_qlnh.AccountExists(CurrentStk))
        return NotFound(new { message = "Khong tim thay tai khoan" });
    if (kq == -1)
    {
        return StatusCode(500);
    }
    return Ok(new
    {
        stk= CurrentStk,
        soDu = kq
    });
    }
    [HttpGet("TransactionHistory")]
        public IActionResult GetTransactionHistory()
        { if (!_qlnh.AccountExists(CurrentStk))
        return NotFound(new { message = "Khong tim thay tai khoan" });
    var kq = _qlnh.GetTransactionHistory(CurrentStk);
    return Ok(kq);
        }
    }