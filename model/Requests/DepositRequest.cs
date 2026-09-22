using System.ComponentModel.DataAnnotations;
namespace HuanBank.Api.Request;
public class DepositRequest
{
    [Required]
    public required decimal Amount{get;set;}
}