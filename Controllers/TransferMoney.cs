using Microsoft.AspNetCore.Mvc;
[ApiController]
[Route("[controller]")]
public class TransferMoneyController : ControllerBase
{
    private readonly BankManager qlnh;
    public TransferMoneyController(BankManager qlnh)
    {
        this.qlnh=qlnh;
    } 
    [HttpPost]
    public IActionResult TransferMoney([FromBody] TfRequest req)
    {
        var kq = qlnh.TransferMoney(req.stk1, req.stk2, req.a);
        return kq switch
        {
            TransactionResult.Success=>
            Ok(new {message="Chuyen tien thanh cong"}),
        TransactionResult.AccountNotFound=>
            NotFound(new {message="Khong tim thay tai khoan"}),
        TransactionResult.InvalidAmount=>
            BadRequest(new {message="So tien khong hop le"}) ,
        TransactionResult.InsufficientBalance=>
            BadRequest(new{message = "So du khong du"}),
        TransactionResult.CannotTransferToSelf=>
            BadRequest(new { message = "Khong the chuyen tien cho chinh minh" }),
        _=> StatusCode(500)
        };
    }
}
public record TfRequest(string stk1, string stk2, decimal a );