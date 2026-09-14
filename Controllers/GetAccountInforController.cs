    using Microsoft.AspNetCore.Mvc;
    [Controller]
    [Route("[controller]")]
    public class GetAccountInforController : ControllerBase
    {
        private readonly BankManager qlnh;
        public GetAccountInforController(BankManager qlnh)
        {
            this.qlnh=qlnh;
        }
        [HttpGet]
        public IActionResult GetAccountInfor(string stk)
        { if (!qlnh.AccountExists(stk))
            return NotFound(new { message = "Khong tim thay tai khoan" });

        var kq = qlnh.GetAccountInfor(stk);
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
    }