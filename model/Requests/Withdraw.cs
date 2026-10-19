using System.ComponentModel.DataAnnotations;
namespace HuanBank.Api.Model.Request;
public class WithdrawRequest
{
    [Required]
    public required decimal Amount{get;set;}
}