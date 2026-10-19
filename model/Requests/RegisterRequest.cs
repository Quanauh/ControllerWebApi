namespace HuanBank.Api.Model.Request;
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
    public required string DateOfBirth {get;set;}
    public string? Address {get;set;}
    [Required]
    public required string AccountNumber {get;set;}
    [Required]
    public required string Password {get;set;}
}