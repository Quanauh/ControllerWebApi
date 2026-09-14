    using Microsoft.AspNetCore.Mvc;
    [Controller]
    [Route("[controller]")]
    public class GetTransactionHistoryController : ControllerBase
    {
        private readonly BankManager qlnh;
        public GetTransactionHistoryController(BankManager qlnh)
        {
            this.qlnh=qlnh;
        }
        [HttpGet]
        public IActionResult GetAccountInfor(string stk)
        { if (!qlnh.AccountExists(stk))
        return NotFound(new { message = "Khong tim thay tai khoan" });
    var kq = qlnh.GetTransactionHistory(stk);
    return Ok(kq);
        }
    }