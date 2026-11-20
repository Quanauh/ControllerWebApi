using HuanBank.Api.DAO.Models;
using HuanBank.Api.Authorization;
namespace HuanBank.Api.DAO;

public interface IAccountDAO
{
    public abstract bool Exists(string AccountNumber);
    LoginAccountRecord? GetLoginAccount(string accountNumber);
    RoleRecord? GetActiveRole(string accountNumber);
    RoleRecord? GetRole(string roleName);
    bool HasPermission(string role, string permissionCode);
    IReadOnlyList<AbacRule> GetAbacRules(string role, string actionCode);
    void CreateAccount(CreateAccountData account);
    BalanceOperationResult Deposit(string AccountNumber, decimal a);
    BalanceOperationResult TryWithdraw(string AccountNumber, decimal a);
    TransferExecutionResult TryTransferMoney(string AccountNumber1, string AccountNumber2, decimal a);
    decimal GetBalance(string AccountNumber);
    AccountRecord? GetAccountInfor(string AccountNumber);
    List<TransactionHistoryRecord> GetTransactionHistory(string AccountNumber);
}
