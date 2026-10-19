namespace HuanBank.Api.DAO.Models;

public class CreateAccountData
{
    public required string Name { get; set; }
    public required string Phone { get; set; }
    public required string Email { get; set; }
    public required string DateOfBirth { get; set; }
    public string? Address { get; set; }
    public required string AccountNumber { get; set; }
    public required string Password { get; set; }
}

public class AccountRecord
{
    public string? AccountNumber { get; set; }
    public string? FullName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Address { get; set; }
    public decimal Balance { get; set; }
}

public class TransactionHistoryRecord
{
    public string AccountNumber { get; set; } = "";
    public string? CounterAccount { get; set; }
    public string TransactionType { get; set; } = "";
    public decimal Amount { get; set; }
    public decimal BalanceBefore { get; set; }
    public decimal BalanceAfter { get; set; }
    public DateTime TransactionDate { get; set; }
}
