using HuanBank.Api.Services.Models;
using HuanBank.Api.Result;
namespace HuanBank.Api.Services;

public interface IBankService
{
    bool AccountExists(string stk);
    RegistrationResult CreateAccount(RegisterAccountCommand command);
    LoginResult Login(string stk, string mk);
    TransactionResult Deposit(string stk, decimal a);
    TransactionResult Withdraw(string stk, decimal a);
    TransactionResult TransferMoney(string stk1, string stk2, decimal a);
    decimal GetBalance(string stk);
    AccountDetails? GetAccountInfor(string stk);
    List<TransactionHistoryItem> GetTransactionHistory(string stk);
}
