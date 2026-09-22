using System.ComponentModel.DataAnnotations;
namespace HuanBank.Api.Request;
public class TranformerRequest
{
    [Required]
    public required string CounterAccount{get;set;}
    [Required]
    public required decimal Amount{get;set;}
}