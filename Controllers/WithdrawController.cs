using Microsoft.AspNetCore.Mvc;
[ApiController]
[Route("[controller]")]
public class WithdrawController:ControllerBase
{
    private readonly BankManager qlnh;
    public WithdrawController(BankManager qlnh)
    {
        this.qlnh=qlnh;
    }
    [HttpPost]
    public IActionResult Withdraw([FromBody] WtRequest req)
    {
        var kq=qlnh.Withdraw(req.stk,req.a);
        return kq switch
        {
            TransactionResult.AccountNotFound=>
                NotFound(new{ message="Khong tim thay tai khoan"}),
            TransactionResult.InvalidAmount=>
                BadRequest(new{message = "So tien khong hop le"}),
            TransactionResult.InsufficientBalance=>
                BadRequest(new{message = "So du khong du"}),
            _=>StatusCode(500)  
        };
    }
}
public record WtRequest(string stk, decimal a);