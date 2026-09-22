using System.ComponentModel.DataAnnotations;
namespace HuanBank.Api.Request;
public class LoginRequest
{
    [Required]
    public required string stk {get;set;}
    [Required]
    public required string mk {get;set;}
}