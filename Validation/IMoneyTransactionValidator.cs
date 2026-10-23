using HuanBank.Api.Result;
namespace HuanBank.Api.Validators;
public interface IMoneyTransactionValidator
{
    MoneyTransactionValidationResult ValidateAmount(decimal amount);

    MoneyTransactionValidationResult ValidateTransfer(
        string sourceAccount,
        string targetAccount,
        decimal amount);
}