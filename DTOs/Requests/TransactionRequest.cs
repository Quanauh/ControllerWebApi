using System.ComponentModel.DataAnnotations;
namespace HuanBank.Api.Model.Request;
public class WithdrawRequest
{
    [Required]
    public required decimal Amount{get;set;}
}
public class TransferRequest
{
    [Required]
    public required string CounterAccount{get;set;}
    [Required]
    public required decimal Amount{get;set;}
}
public class DepositRequest
{
    [Required]
    public required decimal Amount{get;set;}
}