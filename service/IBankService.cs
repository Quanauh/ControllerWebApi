using HuanBank.Api.Services.Models;
using HuanBank.Api.Result;
namespace HuanBank.Api.Services;

public interface IBankService
{
    bool AccountExists(string stk);
    RegistrationResult CreateAccount(RegisterAccountCommand command);
    LoginOutcome Login(string stk, string mk);
    BusinessResponse Deposit(string stk, decimal a);
    BusinessResponse Withdraw(string stk, decimal a);
    BusinessResponse TransferMoney(string stk1, string stk2, decimal a);
    decimal GetBalance(string stk);
    AccountDetails? GetAccountInfor(string stk);
    List<TransactionHistoryItem> GetTransactionHistory(string stk);
}
