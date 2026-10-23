namespace HuanBank.Api.Result;
public enum MoneyTransactionValidationResult
{
    Success,
    InvalidAmount,
    CannotTransferToSelf
}
public enum RegistrationValidationResult
{
    Success,
    MissingRequiredField,
    InvalidDate
}