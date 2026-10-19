using System.ComponentModel.DataAnnotations;
namespace HuanBank.Api.Model.Request;
public class DepositRequest
{
    [Required]
    public required decimal Amount{get;set;}
}