namespace HuanBank.Api.Request;
using System.ComponentModel.DataAnnotations;
public class RegisterRequest
{
    [Required]
    public required string Name {get;set;}
    [Required]
    public required string Phone {get;set;}
    [Required]
    public required string Email {get;set;}
    [Required]
    public required string Dateofbirth {get;set;}
    public string? Adress {get;set;}
    [Required]
    public required string AccountNumber {get;set;}
    [Required]
    public required string Password {get;set;}
}