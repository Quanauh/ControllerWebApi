namespace HuanBank.Api.Validators;

using HuanBank.Api.Result;
public sealed class MoneyTransactionValidator : IMoneyTransactionValidator
{
    public MoneyTransactionValidationResult ValidateAmount(decimal amount)
    {
        return amount <= 0
            ? MoneyTransactionValidationResult.InvalidAmount
            : MoneyTransactionValidationResult.Success;
    }

    public MoneyTransactionValidationResult ValidateTransfer(
        string sourceAccount,
        string targetAccount,
        decimal amount)
    {
        if (string.Equals(sourceAccount, targetAccount, StringComparison.OrdinalIgnoreCase))
        {
            return MoneyTransactionValidationResult.CannotTransferToSelf;
        }

        return ValidateAmount(amount);
    }
}
