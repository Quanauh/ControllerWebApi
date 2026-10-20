using System.ComponentModel.DataAnnotations;
namespace HuanBank.Api.Model.Request;
public class LoginRequest
{
    [Required]
    public required string stk {get;set;}
    [Required]
    public required string mk {get;set;}
}
public class RegisterRequest
{
    [Required]
    public required string Name {get;set;}
    [Required]
    public required string Phone {get;set;}
    [Required]
    public required string Email {get;set;}
    [Required]
    public required string DateOfBirth {get;set;}
    public string? Address {get;set;}
    [Required]
    public required string AccountNumber {get;set;}
    [Required]
    public required string Password {get;set;}
}