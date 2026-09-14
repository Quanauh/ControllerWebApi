using Microsoft.AspNetCore.Mvc;
[ApiController]
[Route("[controller]")]
public class DepositController : ControllerBase
{
    private readonly BankManager qlnh;
    public DepositController(BankManager qlnh)
    {
        this.qlnh=qlnh;
    }
    [HttpPost]
    public IActionResult Deposit([FromBody] DpRequest req)
    {
        var kq=qlnh.Deposit(req.stk,req.a);
        return kq switch
        {
             TransactionResult.AccountNotFound =>
            NotFound(new { message = "Khong tim thay tai khoan" }),
        TransactionResult.InvalidAmount=>
            BadRequest(new{message = "So tien khong hop le"}),
        TransactionResult.Success =>
            Ok(new {message="Nap tien thanh cong",soDu = qlnh.GetBalance(req.stk)}),
            _=>StatusCode(500)
        };
    }
}
public record DpRequest(string stk,decimal a);