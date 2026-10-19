namespace HuanBank.Api.Validators;

using HuanBank.Api.Result;
using HuanBank.Api.Services.Models;
public interface IAccountValidator
{
    TransactionResult ValidateRegistration(RegisterAccountCommand command);
    TransactionResult ValidateAmount(decimal amount);
    TransactionResult ValidateWithdraw(decimal amount, decimal currentBalance);
    TransactionResult ValidateTransfer(string stk1, string stk2, decimal amount, decimal currentBalance);
}
