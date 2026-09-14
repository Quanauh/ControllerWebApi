using Microsoft.AspNetCore.Mvc;
[Controller]
[Route("[controller]")]
public class GetBalanceController : ControllerBase
{
    private readonly BankManager qlnh;
    public GetBalanceController(BankManager qlnh)
    {
        this.qlnh=qlnh;
    }
    [HttpGet]
    public IActionResult GetBalance(string stk)
    {
            var kq=qlnh.GetBalance(stk);
            if (!qlnh.AccountExists(stk))
        return NotFound(new { message = "Khong tim thay tai khoan" });
    if (kq == -1)
    {
        return StatusCode(500);
    }
    return Ok(new
    {
        stk= stk,
        soDu = kq
    });
    }
}