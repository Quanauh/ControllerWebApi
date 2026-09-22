using System.ComponentModel.DataAnnotations;
namespace HuanBank.Api.Request;
public class WithdrawRequest
{
    [Required]
    public required decimal Amount{get;set;}
}