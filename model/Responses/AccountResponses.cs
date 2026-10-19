namespace HuanBank.Api.Model.Responses;

public class AccountInfoResponse
{
    public string? AccountNumber { get; set; }
    public string? FullName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? DateOfBirth { get; set; }
    public string? Address { get; set; }
    public decimal Balance { get; set; }
}

public class BalanceResponse
{
    public required string Stk { get; set; }
    public decimal SoDu { get; set; }
}

public class TransactionHistoryResponse
{
    public string AccountNumber { get; set; } = "";
    public string? CounterAccount { get; set; }
    public string TransactionType { get; set; } = "";
    public decimal Amount { get; set; }
    public decimal BalanceBefore { get; set; }
    public decimal BalanceAfter { get; set; }
    public DateTime TransactionDate { get; set; }
    public string Direction { get; set; } = "";
}
