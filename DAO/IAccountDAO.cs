using HuanBank.Api.DAO.Models;
namespace HuanBank.Api.DAO;

public interface IAccountDAO
{
    public abstract bool Exists(string AccountNumber);
    public abstract bool checkPassWord(string AccountNumber, string Password);
    void CreateAccount(CreateAccountData account);
    void Deposit(string AccountNumber, decimal a);
    bool TryWithdraw(string AccountNumber, decimal a);
    bool TryTransferMoney(string AccountNumber1, string AccountNumber2, decimal a);
    decimal GetBalance(string AccountNumber);
    AccountRecord? GetAccountInfor(string AccountNumber);
    List<TransactionHistoryRecord> GetTransactionHistory(string AccountNumber);
}
