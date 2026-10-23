namespace HuanBank.Api.Result;

public enum RegistrationResult
{
    Success,
    MissingRequiredField,
    InvalidDate,
    AccountAlreadyExists,
    PhoneAlreadyExists,
    EmailAlreadyExists
}

public enum LoginResult
{
    Success,
    AccountNotFound,
    IncorrectPassword
}

public enum TransactionResult
{
    Success,
    AccountNotFound,
    InvalidAmount,
    InsufficientBalance,
    CannotTransferToSelf
}