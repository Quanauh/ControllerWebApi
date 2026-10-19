using System.Globalization;
using HuanBank.Api.Result;
using HuanBank.Api.Services.Models;
namespace HuanBank.Api.Validators;

public class AccountValidator : IAccountValidator
{
    public TransactionResult ValidateRegistration(RegisterAccountCommand command)
    {
            if (!DateTime.TryParseExact(command.DateOfBirth, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            return TransactionResult.InvalidDate;

        return TransactionResult.Success;
    }

    public TransactionResult ValidateAmount(decimal amount)
    {
        return amount <= 0 ? TransactionResult.InvalidAmount : TransactionResult.Success;
    }

    public TransactionResult ValidateWithdraw(decimal amount, decimal currentBalance)
    {
        var amountCheck = ValidateAmount(amount);
        if (amountCheck != TransactionResult.Success) return amountCheck;

        return amount > currentBalance ? TransactionResult.InsufficientBalance : TransactionResult.Success;
    }

    public TransactionResult ValidateTransfer(string stk1, string stk2, decimal amount, decimal currentBalance)
    {
        if (stk1 == stk2) return TransactionResult.CannotTransferToSelf;

        return ValidateWithdraw(amount, currentBalance); // cung logic: so tien hop le + du so du
    }
}
