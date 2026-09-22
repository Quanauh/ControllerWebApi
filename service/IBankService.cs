using HuanBank.Api.Entities;
namespace HuanBank.Api.Services;
public interface IBankService
{
    bool AccountExists(string stk);
    TransactionResult CreateAccount(string ten, string sdt, string email, string ngaysinh, string diachi, string stk, string mk);
    TransactionResult Login(string stk, string mk);
    TransactionResult Deposit(string stk, decimal a);
    TransactionResult Withdraw(string stk, decimal a);
    TransactionResult TransferMoney(string stk1, string stk2, decimal a);
    decimal GetBalance(string stk);
    AccountInfo? GetAccountInfor(string stk);
    List<TransactionHistory> GetTransactionHistory(string stk);
}